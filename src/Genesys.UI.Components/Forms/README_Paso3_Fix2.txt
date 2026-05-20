GenesysGridViews Paso 3 Fix 2
=============================

Cambios incluidos:

1. GenesysGridConfigurator
   - Ya no usa FooterRowCount para summaries.
   - FooterRowCount congela filas reales del DataSource y por eso aparecia el ultimo registro como si fuera summary.
   - Ahora el summary tipo Excel se mantiene exclusivamente con TableSummaryRows.
   - Antes de asignar un nuevo DataTable se limpia estado visual anterior: summaries, grupos, ordenamientos, DataSource y columnas.
   - Esto evita errores internos de Syncfusion cuando el nuevo DataTable tiene menos columnas o el grid conserva indices visuales viejos.

2. GenesysGridForm
   - El panel del diseñador ya no se empalma sobre el grid.
   - Se creo GridWorkspacePanel con dos columnas: grid Fill + designer panel fijo a la derecha.
   - Al mostrar el diseñador, el grid reduce su espacio disponible.
   - Al cerrar el diseñador, la columna derecha vuelve a ancho 0.

3. GenesysGridViewDesignerPanel
   - Se agrego boton Cerrar en la parte inferior, ademas del boton X superior.

Nota:
- El summary se ve como fila inferior del SfDataGrid usando TableSummaryRows.
- Esta version prioriza alineacion correcta con columnas, scroll horizontal, columnas movidas/ocultas y resize.
- No usa FooterRowCount porque en Syncfusion ese mecanismo congela registros del detalle, no summaries.
