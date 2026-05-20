Genesys Grid Views - Paso 3
===========================

Este paquete agrega el Diseñador de Vista lateral al Paso 2.1.

Archivos nuevos:
- Controls/GridViews/GenesysGridColumnProfile.cs
- Controls/GridViews/GenesysGridViewDesignerPanel.cs

Archivos actualizados:
- Forms/GenesysGridForm.cs
- Controls/GridViews/GenesysGridViewManager.cs
- Controls/GridViews/GenesysGridConfigurator.cs

Funcionalidad:
- Opción nueva en el menú de tres puntos: "Diseñar vista...".
- Panel lateral derecho no modal.
- Selección de columna.
- Visible / título / decimales / formato / alineación / summary / agrupar / congelar / ancho.
- Aplicación en vivo sobre el SfDataGrid.
- Summary estilo Excel usando GridTableSummaryRow dentro del grid.
- FooterRowCount se aplica por reflexión para mantener compatibilidad entre builds de Syncfusion.
- El scroll horizontal sigue alineado porque el summary vive dentro del mismo SfDataGrid.

Notas:
- "Congelar" usa la propiedad FrozenColumnCount mediante reflexión. Si el build de Syncfusion usado no la expone, simplemente no hace nada y no rompe compilación.
- El botón "Guardar vista" en el diseñador guarda la vista actual o pide nombre si estás en Predeterminada.
