# Contrato funcional de Furvo V1.0

Estado: **alcance funcional aprobado**. V1.0 será la primera versión comercial, no una entrega interna. Debe ofrecer un mundo futbolístico autónomo, persistente y estable durante muchas temporadas. Cada sistema tendrá una implementación mínima funcional. Este contrato registra requisitos futuros; no declara que estén implementados en V0.1, entrega 4.

## Sistemas imprescindibles

| Sistema | Compromiso mínimo V1.0 |
| --- | --- |
| Competiciones | Liga Cumbre de España de 20 clubes y Segunda ficticia de otros 20; calendarios, resultados, clasificaciones, temporadas consecutivas e historial. Tres ascensos y tres descensos por temporada. Segunda con simulación simplificada. |
| Continuidad entre divisiones | Ascendidos y descendidos conservan identidad, jugadores, contratos, economía e historial. Todo ascendido puede participar plenamente en Primera. |
| Partidos y tácticas | Motor determinista; alineaciones automáticas/manuales; formación, mentalidad e instrucciones básicas por línea; eventos y estadísticas textuales. Persistir resultados y eventos y reproducir mediante semilla, entradas y versión del motor. |
| Mercado | Compras, ventas, contratos, renovaciones, jugadores libres, ventanas y negociación básica. Agentes representados mediante exigencias contractuales. |
| Economía | Presupuesto, ingresos, gastos, salarios y balance; riesgo de insolvencia y consecuencias de mala gestión. |
| IA de clubes | Elegir alineaciones, comprar/vender, renovar, gestionar plantilla y presupuesto, cambiar entrenador y adaptarse a ascensos, descensos y resultados. |
| Futbolistas | Evolución y declive de atributos por edad, lesiones, retiradas y generación anual de juveniles. Mantener plantillas viables durante décadas. |
| Entrenador/directiva | Creación e identidad del entrenador, objetivos, evaluación, despidos y cambios de entrenador; continuar la partida después de un despido. |
| Aplicación comercial | Interfaz completa en español, guardado/carga fiables, migraciones versionadas, errores gestionados, paquete Windows y revisión de licencias de recursos. |

## Sistemas en versión mínima

- Entrenamientos: planes sencillos por bloques.
- Moral: valor individual influido por minutos y resultados.
- Vestuario: capitanes y líderes, sin conflictos complejos.
- Agentes: exigencias básicas en negociaciones.
- Infraestructuras: nivel numérico del estadio y del centro de entrenamiento.
- Partidos: narración textual, eventos y estadísticas; sin representación 2D.

## Exclusiones de V1.0

Copa del Rey y otras copas; competiciones internacionales; selecciones nacionales; otros países y ligas; épocas históricas; regulaciones cambiantes; medios complejos; conflictos profundos de vestuario; representación 2D; construcción detallada de instalaciones; agentes avanzados; gestión exhaustiva de cantera.

La generación anual mínima de juveniles es obligatoria y no equivale a gestionar una cantera exhaustiva. No se generan ahora los veinte clubes de Segunda ni sus plantillas.

## Criterios medibles de publicación

| Puerta de publicación | Evidencia exigida |
| --- | --- |
| Continuidad | Ejecutar 30 temporadas consecutivas, con cero errores críticos. Archivar semilla, versión, registros y resultados. |
| Viabilidad del mundo | Medir por temporada edades, tamaños/composición de plantillas, cobertura contractual y finanzas; demostrar ausencia de degeneración sistemática. Fijar y registrar rangos/umbrales de aceptación antes de evaluar, sin cambiarlos para ocultar fallos. |
| Movilidad | Verificar exactamente tres ascensos y tres descensos en cada transición; 20 clubes en cada división e identidades e historiales conservados. |
| Integridad | Cero referencias huérfanas o contratos incompatibles; identidades estables y continuidad de clubes/jugadores. |
| Persistencia | Guardar y reabrir en todos los puntos admitidos; comparar el estado persistente y continuar sin divergencia. |
| Compatibilidad | Probar todas las migraciones de formatos declarados compatibles sobre copias, conservando el original. |
| Regresión | Todas las pruebas automatizadas obligatorias aprobadas, sin desactivar pruebas para publicar. |
| Determinismo | Superar reproducibilidad y referencia versionada de 1.000 partidos conforme a ARCHITECTURE.md. |
| Rendimiento | Definir antes de publicar un límite de tiempo por temporada, equipo de referencia, configuración y procedimiento; medir y cumplirlo. El número no está fijado por este contrato. |
| Beta privada | Al menos cinco probadores diferentes completan tres temporadas o más cada uno, con registros reproducibles de incidencias. |
| Contenido/licencias | Revisar nombres ficticios, marcas y licencias de fuentes, imágenes y demás recursos; registrar procedencia y permisos. |
| Distribución | Validar el ejecutable Windows en distintos equipos; registrar sistema, configuración, resultado y errores. |

La definición previa de límites de rendimiento y de viabilidad es una tarea de validación obligatoria, no permiso para publicar sin medir. Actualmente no se ha acreditado ninguna de estas puertas para V1.0 por el hecho de completar una entrega interna.

## Control de alcance

Si se incorpora una funcionalidad importante a V1.0, se reducirá o aplazará otra de complejidad comparable. No se publicarán sistemas incompletos que comprometan estabilidad. La evolución temporal de reputación sigue pendiente de diseño. Nuevas entregas requieren autorización; el trabajo actual se limita a consolidación y V0.1, entrega 4.
