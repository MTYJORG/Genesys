using System.Collections.Generic;

namespace Genesys.UI.Components.Controls.GridViews
{
    public interface IGenesysGridViewStore
    {
        IList<GenesysGridViewLayout> Load(string gridKey);
        void Save(GenesysGridViewLayout layout);
        void Delete(string gridKey, string viewName);
    }

    public interface IGenesysGridViewStateStore
    {
        string LoadCurrentViewName(string gridKey);
        void SaveCurrentViewName(string gridKey, string viewName);
    }
}
