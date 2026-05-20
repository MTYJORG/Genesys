Genesys Grid Views - Paso 3 Fix 3

Cambios incluidos:

1) Cerrar diseñador sin dejar espacio en blanco
- Se agregó HideViewDesigner().
- El botón Cerrar ahora oculta el panel y regresa la columna derecha del TableLayoutPanel a 0 px.

2) Validación de cambios pendientes al cerrar el formulario
- GenesysGridViewManager expone ConfirmPendingChangesBeforeClose().
- GenesysGridForm.OnFormClosing pregunta si se desean guardar cambios en la vista.
- Si el usuario cancela, se cancela el cierre del formulario.

3) Persistencia de la vista actual
- Se agregó GenesysGridViewState.
- GenesysGridViewFileStore guarda __state.xml con la vista actual.
- Al cargar datos, ReapplyCurrentView() vuelve a aplicar la última vista usada.

4) Summary
- Se mantiene el summary nativo de Syncfusion con GridTableSummaryRow.
- No se volvió a usar FooterRowCount porque congela filas de datos, no summary rows.
- El siguiente paso recomendado es dedicar un Paso 4 al modo Excel Summary completo: fila de totales por columna, robusta ante scroll horizontal, agrupaciones y filtros.
