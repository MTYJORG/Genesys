using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Genesys.Framework;

namespace Genesys.UI.Components.Controls.GridViews
{
    public enum GenesysGridViewScope
    {
        Usuario = 0,
        Compartida = 1,
        Global = 2,
        Sistema = 3
    }

    public interface IGridViewStore
    {
        IList<GridViewLayout> Load(string gridKey);
        void Save(GridViewLayout layout);
        void Delete(string gridKey, string viewName);
    }

    public interface IGenesysGridViewStateStore
    {
        string LoadCurrentViewName(string gridKey);
        void SaveCurrentViewName(string gridKey, string viewName);
    }

    public interface IGenesysGridViewOrderStore
    {
        IList<string> LoadViewOrder(string gridKey);
        void SaveViewOrder(string gridKey, IList<string> orderedViewNames);
    }

    /// <summary>
    /// Persistencia SQL Server + JSON para el módulo de Vistas del SfDataGrid.
    /// Reemplaza la persistencia local en archivos .xml y __state.xml.
    /// Usa AppConfig.ConnectionString como conexión central del framework.
    /// </summary>
    public class GridViewSqlStore : IGridViewStore, IGenesysGridViewStateStore, IGenesysGridViewOrderStore
    {
        private static readonly object schemaSyncRoot = new object();
        private static bool schemaInitialized;

        private readonly string connectionString;
        private readonly string userName;
        private readonly bool autoCreateSchema;

        public GridViewSqlStore()
            : this(AppConfig.ConnectionString, Environment.UserName, true)
        {
        }

        public GridViewSqlStore(string connectionString)
            : this(connectionString, Environment.UserName, true)
        {
        }

        public GridViewSqlStore(string connectionString, string userName, bool autoCreateSchema)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("AppConfig.ConnectionString no está inicializado. Llame AppConfig.Initialize() antes de inicializar el módulo de vistas.");

            this.connectionString = connectionString;
            this.userName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName.Trim();
            this.autoCreateSchema = autoCreateSchema;

            if (this.autoCreateSchema)
                EnsureSchema();
        }

        public IList<GridViewLayout> Load(string gridKey)
        {
            var result = new List<GridViewLayout>();

            if (string.IsNullOrWhiteSpace(gridKey))
                return result;

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT ViewName, LayoutJson, IsDefault, CreatedAt, ModifiedAt
FROM dbo.GenesysGridViews
WHERE GridKey = @GridKey
  AND IsDeleted = 0
  AND (
        Scope IN (@ScopeCompartida, @ScopeGlobal, @ScopeSistema)
        OR UserName = @UserName
      )
ORDER BY IsDefault DESC, DisplayOrder ASC, ViewName ASC;";

                command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                command.Parameters.Add("@ScopeCompartida", SqlDbType.Int).Value = (int)GenesysGridViewScope.Compartida;
                command.Parameters.Add("@ScopeGlobal", SqlDbType.Int).Value = (int)GenesysGridViewScope.Global;
                command.Parameters.Add("@ScopeSistema", SqlDbType.Int).Value = (int)GenesysGridViewScope.Sistema;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string layoutJson = reader["LayoutJson"] as string;
                        GridViewLayout layout = DeserializeLayout(layoutJson);

                        if (layout == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(layout.GridKey))
                            layout.GridKey = gridKey;

                        if (string.IsNullOrWhiteSpace(layout.ViewName))
                            layout.ViewName = Convert.ToString(reader["ViewName"]);

                        if (reader["IsDefault"] != DBNull.Value)
                            layout.IsDefault = Convert.ToBoolean(reader["IsDefault"]);

                        if (reader["CreatedAt"] != DBNull.Value)
                            layout.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);

                        if (reader["ModifiedAt"] != DBNull.Value)
                            layout.ModifiedAt = Convert.ToDateTime(reader["ModifiedAt"]);

                        result.Add(layout);
                    }
                }
            }

            return result
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.ViewName)
                .ToList();
        }

        public void Save(GridViewLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException("layout");

            if (string.IsNullOrWhiteSpace(layout.GridKey))
                throw new ArgumentException("GridKey requerido.");

            if (string.IsNullOrWhiteSpace(layout.ViewName))
                throw new ArgumentException("ViewName requerido.");

            DateTime now = DateTime.Now;

            if (layout.CreatedAt == DateTime.MinValue)
                layout.CreatedAt = now;

            layout.ModifiedAt = now;

            string layoutJson = SerializeLayout(layout);
            int displayOrder = GetNextDisplayOrder(layout.GridKey);

            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                long existingId = GetExistingViewId(connection, layout.GridKey, layout.ViewName);

                if (existingId > 0)
                {
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
UPDATE dbo.GenesysGridViews
SET LayoutJson = @LayoutJson,
    IsDefault = @IsDefault,
    ModifiedAt = @ModifiedAt,
    ModifiedBy = @ModifiedBy,
    IsDeleted = 0
WHERE Id = @Id;";

                        command.Parameters.Add("@LayoutJson", SqlDbType.NVarChar).Value = (object)layoutJson ?? DBNull.Value;
                        command.Parameters.Add("@IsDefault", SqlDbType.Bit).Value = layout.IsDefault;
                        command.Parameters.Add("@ModifiedAt", SqlDbType.DateTime).Value = now;
                        command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 128).Value = userName;
                        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = existingId;
                        command.ExecuteNonQuery();
                    }

                    return;
                }

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
INSERT INTO dbo.GenesysGridViews
(
    GridKey, ViewName, UserName, Scope,
    IsDefault, IsSystem, IsLocked, IsDeleted,
    DisplayOrder, LayoutJson,
    CreatedAt, ModifiedAt, CreatedBy, ModifiedBy
)
VALUES
(
    @GridKey, @ViewName, @UserName, @Scope,
    @IsDefault, 0, 0, 0,
    @DisplayOrder, @LayoutJson,
    @CreatedAt, @ModifiedAt, @CreatedBy, @ModifiedBy
);";

                    command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = layout.GridKey;
                    command.Parameters.Add("@ViewName", SqlDbType.NVarChar, 200).Value = layout.ViewName;
                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                    command.Parameters.Add("@Scope", SqlDbType.Int).Value = (int)GenesysGridViewScope.Usuario;
                    command.Parameters.Add("@IsDefault", SqlDbType.Bit).Value = layout.IsDefault;
                    command.Parameters.Add("@DisplayOrder", SqlDbType.Int).Value = displayOrder;
                    command.Parameters.Add("@LayoutJson", SqlDbType.NVarChar).Value = (object)layoutJson ?? DBNull.Value;
                    command.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = layout.CreatedAt;
                    command.Parameters.Add("@ModifiedAt", SqlDbType.DateTime).Value = layout.ModifiedAt;
                    command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 128).Value = userName;
                    command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 128).Value = userName;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string gridKey, string viewName)
        {
            if (string.IsNullOrWhiteSpace(gridKey) || string.IsNullOrWhiteSpace(viewName))
                return;

            string normalizedViewName = viewName.Trim();

            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Eliminación física robusta:
                        // - borra todas las filas del usuario para esa vista;
                        // - también borra filas antiguas que hayan quedado con UserName NULL;
                        // - evita que una vista duplicada por migraciones/pruebas siga apareciendo.
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
DELETE FROM dbo.GenesysGridViews
WHERE GridKey = @GridKey
  AND LTRIM(RTRIM(ViewName)) = @ViewName
  AND (
        UserName = @UserName
        OR UserName IS NULL
      );";

                            command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                            command.Parameters.Add("@ViewName", SqlDbType.NVarChar, 200).Value = normalizedViewName;
                            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                            command.ExecuteNonQuery();
                        }

                        // Si el usuario tenía como activa la vista eliminada, regresar a Predeterminada.
                        // Se compara con Trim para cubrir nombres guardados con espacios por versiones previas.
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
UPDATE dbo.GenesysGridViewState
SET CurrentViewName = @DefaultViewName,
    ModifiedAt = @ModifiedAt
WHERE GridKey = @GridKey
  AND UserName = @UserName
  AND LTRIM(RTRIM(CurrentViewName)) = @ViewName;";

                            command.Parameters.Add("@DefaultViewName", SqlDbType.NVarChar, 200).Value = "Predeterminada";
                            command.Parameters.Add("@ModifiedAt", SqlDbType.DateTime).Value = DateTime.Now;
                            command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                            command.Parameters.Add("@ViewName", SqlDbType.NVarChar, 200).Value = normalizedViewName;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
            }
        }

        public string LoadCurrentViewName(string gridKey)
        {
            if (string.IsNullOrWhiteSpace(gridKey))
                return null;

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 1 CurrentViewName
FROM dbo.GenesysGridViewState
WHERE GridKey = @GridKey
  AND UserName = @UserName;";

                command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;

                connection.Open();
                object value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value);
            }
        }

        public void SaveCurrentViewName(string gridKey, string viewName)
        {
            if (string.IsNullOrWhiteSpace(gridKey))
                return;

            viewName = string.IsNullOrWhiteSpace(viewName) ? "Predeterminada" : viewName.Trim();

            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                bool exists;
                using (SqlCommand existsCommand = connection.CreateCommand())
                {
                    existsCommand.CommandText = @"
SELECT COUNT(1)
FROM dbo.GenesysGridViewState
WHERE GridKey = @GridKey
  AND UserName = @UserName;";

                    existsCommand.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                    existsCommand.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                    exists = Convert.ToInt32(existsCommand.ExecuteScalar()) > 0;
                }

                using (SqlCommand command = connection.CreateCommand())
                {
                    if (exists)
                    {
                        command.CommandText = @"
UPDATE dbo.GenesysGridViewState
SET CurrentViewName = @CurrentViewName,
    ModifiedAt = @ModifiedAt
WHERE GridKey = @GridKey
  AND UserName = @UserName;";
                    }
                    else
                    {
                        command.CommandText = @"
INSERT INTO dbo.GenesysGridViewState
(
    GridKey, UserName, CurrentViewName, ModifiedAt
)
VALUES
(
    @GridKey, @UserName, @CurrentViewName, @ModifiedAt
);";
                    }

                    command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                    command.Parameters.Add("@CurrentViewName", SqlDbType.NVarChar, 200).Value = viewName;
                    command.Parameters.Add("@ModifiedAt", SqlDbType.DateTime).Value = DateTime.Now;
                    command.ExecuteNonQuery();
                }
            }
        }

        public IList<string> LoadViewOrder(string gridKey)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(gridKey))
                return result;

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT ViewName
FROM dbo.GenesysGridViews
WHERE GridKey = @GridKey
  AND UserName = @UserName
  AND Scope = @Scope
  AND IsDeleted = 0
ORDER BY DisplayOrder ASC, ViewName ASC;";

                command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                command.Parameters.Add("@Scope", SqlDbType.Int).Value = (int)GenesysGridViewScope.Usuario;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string viewName = Convert.ToString(reader["ViewName"]);
                        if (!string.IsNullOrWhiteSpace(viewName))
                            result.Add(viewName);
                    }
                }
            }

            return result;
        }

        public void SaveViewOrder(string gridKey, IList<string> orderedViewNames)
        {
            if (string.IsNullOrWhiteSpace(gridKey) || orderedViewNames == null)
                return;

            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();

                int order = 1;
                foreach (string viewName in orderedViewNames)
                {
                    if (string.IsNullOrWhiteSpace(viewName))
                        continue;

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
UPDATE dbo.GenesysGridViews
SET DisplayOrder = @DisplayOrder,
    ModifiedAt = @ModifiedAt,
    ModifiedBy = @ModifiedBy
WHERE GridKey = @GridKey
  AND ViewName = @ViewName
  AND UserName = @UserName
  AND Scope = @Scope
  AND IsDeleted = 0;";

                        command.Parameters.Add("@DisplayOrder", SqlDbType.Int).Value = order;
                        command.Parameters.Add("@ModifiedAt", SqlDbType.DateTime).Value = DateTime.Now;
                        command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 128).Value = userName;
                        command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                        command.Parameters.Add("@ViewName", SqlDbType.NVarChar, 200).Value = viewName.Trim();
                        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                        command.Parameters.Add("@Scope", SqlDbType.Int).Value = (int)GenesysGridViewScope.Usuario;
                        command.ExecuteNonQuery();
                    }

                    order++;
                }
            }
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(connectionString);
        }

        private long GetExistingViewId(SqlConnection connection, string gridKey, string viewName)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 1 Id
FROM dbo.GenesysGridViews
WHERE GridKey = @GridKey
  AND ViewName = @ViewName
  AND UserName = @UserName
  AND Scope = @Scope;";

                command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                command.Parameters.Add("@ViewName", SqlDbType.NVarChar, 200).Value = viewName;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                command.Parameters.Add("@Scope", SqlDbType.Int).Value = (int)GenesysGridViewScope.Usuario;

                object value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0L : Convert.ToInt64(value);
            }
        }

        private int GetNextDisplayOrder(string gridKey)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT ISNULL(MAX(DisplayOrder), 0) + 1
FROM dbo.GenesysGridViews
WHERE GridKey = @GridKey
  AND UserName = @UserName
  AND Scope = @Scope
  AND IsDeleted = 0;";

                command.Parameters.Add("@GridKey", SqlDbType.NVarChar, 300).Value = gridKey;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 128).Value = userName;
                command.Parameters.Add("@Scope", SqlDbType.Int).Value = (int)GenesysGridViewScope.Usuario;

                connection.Open();
                object value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 1 : Convert.ToInt32(value);
            }
        }

        private static string SerializeLayout(GridViewLayout layout)
        {
            if (layout == null)
                return null;

            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(GridViewLayout));
                serializer.WriteObject(stream, layout);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static GridViewLayout DeserializeLayout(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(GridViewLayout));
                    return serializer.ReadObject(stream) as GridViewLayout;
                }
            }
            catch
            {
                return null;
            }
        }

        private void EnsureSchema()
        {
            if (schemaInitialized)
                return;

            lock (schemaSyncRoot)
            {
                if (schemaInitialized)
                    return;

                using (SqlConnection connection = CreateConnection())
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = GetSchemaSql();
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                schemaInitialized = true;
            }
        }

        private static string GetSchemaSql()
        {
            return @"
IF OBJECT_ID('dbo.GenesysGridViews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GenesysGridViews
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GenesysGridViews PRIMARY KEY,
        GridKey NVARCHAR(300) NOT NULL,
        ViewName NVARCHAR(200) NOT NULL,
        UserName NVARCHAR(128) NULL,
        Scope INT NOT NULL CONSTRAINT DF_GenesysGridViews_Scope DEFAULT(0),
        IsDefault BIT NOT NULL CONSTRAINT DF_GenesysGridViews_IsDefault DEFAULT(0),
        IsSystem BIT NOT NULL CONSTRAINT DF_GenesysGridViews_IsSystem DEFAULT(0),
        IsLocked BIT NOT NULL CONSTRAINT DF_GenesysGridViews_IsLocked DEFAULT(0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_GenesysGridViews_IsDeleted DEFAULT(0),
        DisplayOrder INT NOT NULL CONSTRAINT DF_GenesysGridViews_DisplayOrder DEFAULT(0),
        LayoutJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_GenesysGridViews_CreatedAt DEFAULT(GETDATE()),
        ModifiedAt DATETIME NOT NULL CONSTRAINT DF_GenesysGridViews_ModifiedAt DEFAULT(GETDATE()),
        CreatedBy NVARCHAR(128) NULL,
        ModifiedBy NVARCHAR(128) NULL
    );

    CREATE UNIQUE INDEX UX_GenesysGridViews_Key
        ON dbo.GenesysGridViews(GridKey, ViewName, UserName, Scope);
END;

IF OBJECT_ID('dbo.GenesysGridViewState', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GenesysGridViewState
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GenesysGridViewState PRIMARY KEY,
        GridKey NVARCHAR(300) NOT NULL,
        UserName NVARCHAR(128) NOT NULL,
        CurrentViewName NVARCHAR(200) NULL,
        ModifiedAt DATETIME NOT NULL CONSTRAINT DF_GenesysGridViewState_ModifiedAt DEFAULT(GETDATE())
    );

    CREATE UNIQUE INDEX UX_GenesysGridViewState_Key
        ON dbo.GenesysGridViewState(GridKey, UserName);
END;
";
        }
    }

public class GridViewPersistenceService
    {
        private readonly IWin32Window owner;
        private readonly string gridKey;
        private readonly IGridViewStore store;
        private readonly IGenesysGridViewStateStore stateStore;
        private readonly string defaultViewName;
        private readonly Func<string> getCurrentViewName;
        private readonly Action<string> setCurrentViewName;
        private readonly Func<string, bool> isDefaultView;
        private readonly Func<string, GridViewLayout> captureLayout;
        private readonly Action applyDefaultLayout;
        private readonly Action updateButtonState;
        private readonly Action<bool> setHasChanges;

        public GridViewPersistenceService(
            IWin32Window owner,
            string gridKey,
            IGridViewStore store,
            IGenesysGridViewStateStore stateStore,
            string defaultViewName,
            Func<string> getCurrentViewName,
            Action<string> setCurrentViewName,
            Func<string, bool> isDefaultView,
            Func<string, GridViewLayout> captureLayout,
            Action applyDefaultLayout,
            Action updateButtonState,
            Action<bool> setHasChanges)
        {
            this.owner = owner;
            this.gridKey = gridKey;
            this.store = store;
            this.stateStore = stateStore;
            this.defaultViewName = defaultViewName;
            this.getCurrentViewName = getCurrentViewName;
            this.setCurrentViewName = setCurrentViewName;
            this.isDefaultView = isDefaultView;
            this.captureLayout = captureLayout;
            this.applyDefaultLayout = applyDefaultLayout;
            this.updateButtonState = updateButtonState;
            this.setHasChanges = setHasChanges;
        }

        public IList<GridViewLayout> LoadViews()
        {
            return store.Load(gridKey);
        }

        public string RestoreCurrentViewName(string fallbackViewName)
        {
            if (stateStore == null)
                return fallbackViewName;

            string savedViewName = stateStore.LoadCurrentViewName(gridKey);

            if (string.IsNullOrWhiteSpace(savedViewName))
                return fallbackViewName;

            savedViewName = savedViewName.Trim();

            if (isDefaultView(savedViewName))
                return defaultViewName;

            IList<GridViewLayout> views = LoadViews();
            bool exists = views != null && views.Any(x =>
                x != null &&
                string.Equals(x.ViewName, savedViewName, StringComparison.OrdinalIgnoreCase));

            if (exists)
                return savedViewName;

            // Si la vista activa ya no existe, no conservar un estado huérfano.
            // Esto evita que al reabrir el formulario se apliquen filtros/layouts de una vista eliminada.
            PersistCurrentViewName(defaultViewName);
            return defaultViewName;
        }

        public void PersistCurrentViewName(string currentViewName)
        {
            if (stateStore != null)
                stateStore.SaveCurrentViewName(gridKey, currentViewName);
        }

        public bool SaveCurrentView(string viewName)
        {

            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
            {
                bool saveAsResult = SaveAsNewView();
                return saveAsResult;
            }

            var layout = captureLayout(viewName);


            if (layout != null)
            {
            }

            try
            {
                store.Save(layout);
            }
            catch
            {
                throw;
            }

            setCurrentViewName(viewName);
            setHasChanges(false);
            PersistCurrentViewName(viewName);
            updateButtonState();


            return true;
        }

        public bool SaveAsNewView()
        {
            string currentViewName = getCurrentViewName();
            string name = GridViewPrompt.Ask(
                "Nueva vista",
                "Nombre de la vista:",
                isDefaultView(currentViewName) ? string.Empty : currentViewName);

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return SaveCurrentView(name);
        }

        public void DuplicateView()
        {
            string currentViewName = getCurrentViewName();
            string baseName = isDefaultView(currentViewName) ? "Nueva vista" : currentViewName + " copia";
            string name = GridViewPrompt.Ask("Duplicar vista", "Nuevo nombre:", baseName);

            if (string.IsNullOrWhiteSpace(name))
                return;

            SaveCurrentView(name);
        }

        public void DeleteViewFromMenu()
        {
            IList<GridViewLayout> views = LoadViews();

            if (views == null || views.Count == 0)
            {
                MessageBox.Show(owner, "No hay vistas guardadas para eliminar.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string viewName = getCurrentViewName();

            if (isDefaultView(viewName))
                viewName = GridViewPrompt.Ask("Eliminar vista", "Nombre de la vista a eliminar:", string.Empty);

            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
                return;

            bool exists = views.Any(x => string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                MessageBox.Show(owner, "No se encontró la vista '" + viewName + "'.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DeleteView(viewName);
        }

        public void DeleteView(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
                return;

            DialogResult result = MessageBox.Show(
                owner,
                "¿Deseas eliminar la vista '" + viewName + "'? Esta acción no se puede deshacer.",
                "Eliminar vista",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            store.Delete(gridKey, viewName);

            if (string.Equals(getCurrentViewName(), viewName, StringComparison.OrdinalIgnoreCase))
            {
                applyDefaultLayout();
                return;
            }

            updateButtonState();
        }

        public bool SaveCurrentOrAsk()
        {
            string currentViewName = getCurrentViewName();

            if (isDefaultView(currentViewName))
                return SaveAsNewView();

            return SaveCurrentView(currentViewName);
        }
    }


public static class GridViewPrompt
    {
        public static string Ask(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var textBox = new TextBox())
            using (var lbl = new Label())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(360, 120);
                form.Font = new Font("Segoe UI", 9F);

                lbl.Text = label;
                lbl.Left = 12;
                lbl.Top = 12;
                lbl.Width = 330;

                textBox.Left = 12;
                textBox.Top = 38;
                textBox.Width = 330;
                textBox.Text = defaultValue ?? string.Empty;

                ok.Text = "Aceptar";
                ok.Left = 186;
                ok.Top = 78;
                ok.Width = 75;
                ok.DialogResult = DialogResult.OK;

                cancel.Text = "Cancelar";
                cancel.Left = 267;
                cancel.Top = 78;
                cancel.Width = 75;
                cancel.DialogResult = DialogResult.Cancel;

                form.Controls.Add(lbl);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                return form.ShowDialog() == DialogResult.OK
                    ? textBox.Text.Trim()
                    : null;
            }
        }
    }

}
