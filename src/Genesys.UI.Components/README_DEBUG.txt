GenesysGridViews - Final Fix v3

Cambios principales:
1. El panel superior de filtros vuelve a manejar su propia persistencia normal.
   La vista ya no aplica FilterStateXml para no pisar el panel.
2. La vista guarda/restaura el estado nativo del SfDataGrid en NativeGridLayoutXml.
   Esto incluye filtros internos del grid y orden/sort.
3. Cuando cambia el DataSource por filtros superiores, si hay una vista activa se reaplica solo el layout/estado del grid.
4. La vista activa se muestra en el título del formulario y en el tooltip del botón de vistas.
5. Se agregó detección por reflexión de eventos de filtros/sorts del grid para marcar cambios sin guardar cuando la versión de Syncfusion expone esos eventos.
6. La vista Predeterminada intenta limpiar filtros/sorts nativos y volver al estado base capturado antes de aplicar vistas.

Debug útil:
- GRID VIEW MANAGER: ApplyLayout
- NATIVE GRID SERIALIZE
- NATIVE GRID DESERIALIZE START / END
- ReapplyCurrentViewLayoutOnly
- RestoreFiltersAndSearch
- ClearNativeGridRuntimeState
