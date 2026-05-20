Genesys Grid Views - Paso 3 Fix 4

Correcciones:
1. Summary row sin valores:
   - Se normalizaron los tokens de Syncfusion: Sum, Average, Min, Max, Count.
   - Count usa SummaryType.CountAggregate y formato {Count}.
   - Int usa Int32Aggregate, el resto numérico usa DoubleAggregate.
   - Se refresca la vista después de crear/quitar summaries.

2. Error al limpiar agrupaciones:
   - Se reemplazó Clear() directo sobre GroupColumnDescriptions por limpieza inversa segura.
   - Se agregó SafeGridMutation para reintentar cambios estructurales mediante BeginInvoke cuando Syncfusion queda en estado transitorio por drag/group/reorder.

Notas:
- Este fix mantiene el summary nativo de Syncfusion dentro del grid para conservar alineación con scroll horizontal.
- Si una columna decimal sigue sin mostrar summary, revisar que el DataTable tenga tipo decimal/double/int real y no string.
