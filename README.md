# Laboratorio 06: Neptuno

Aplicación WPF para administrar productos, categorías, proveedores y pedidos en NeptunoDB. Incluye búsqueda de proveedores por contacto y ciudad, y un reporte de detalles de pedidos por intervalo de fechas.

## Requisitos

- Windows y Visual Studio con soporte para WPF y .NET 10.
- SQL Server con la instancia indicada en `WPF_SP/dbsettings.local.json`.

## Preparar la base de datos

El archivo [NeptunoDB.sql](NeptunoDB.sql) contiene el esquema y los datos de ejemplo, más un bloque final con la columna `Activo`, los procedimientos almacenados y las correcciones de caracteres acentuados.

- Si todavía no existe la base, ejecuta el archivo completo una sola vez desde SQL Server Management Studio (SSMS).
- Si ya ejecutaste la versión original del script y las tablas existen, ejecuta solamente el bloque final que comienza con `Lab 06 application additions`. No vuelvas a ejecutar la parte que crea las tablas.

## Configurar la conexión

1. Copia `WPF_SP/dbsettings.example.json` y nombra la copia `WPF_SP/dbsettings.local.json`.
2. Edita `Server` con el nombre de tu instancia y confirma que `Database` sea `NeptunoDB`.
3. Para autenticación de Windows, usa `"IntegratedSecurity": true` y deja `User` y `Password` vacíos. Para autenticación de SQL Server, usa `false` e ingresa las credenciales en esos dos campos.
4. Guarda el archivo. Está excluido de Git para no publicar credenciales. Al compilar, se copia junto al ejecutable WPF.

También se aceptan las variables de entorno `NEPTUNO_DB_SERVER`, `NEPTUNO_DB_DATABASE`, `NEPTUNO_DB_INTEGRATED_SECURITY`, `NEPTUNO_DB_USER` y `NEPTUNO_DB_PASSWORD`. Si existen, tienen prioridad sobre el archivo local.

## Ejecutar

Abre `WPF_SP.slnx` en Visual Studio, establece `WPF_SP` como proyecto de inicio y pulsa **F5**. Para ver cambios después de editar la interfaz o la configuración, detén primero la ejecución activa y vuelve a iniciarla.

## Funciones

- Mantenimiento de productos, categorías, proveedores y pedidos.
- Bajas lógicas: los registros se marcan con `Activo = 0`; no se borran físicamente.
- Búsqueda de proveedores por nombre de contacto y ciudad.
- Reporte de detalles de pedidos por fechas, excluyendo pedidos dados de baja.

## Criterio de modo desconectado

Las consultas llenan `DataSet`/`DataTable` mediante `SqlDataAdapter` y cierran la conexión después de cargar los datos. La interfaz trabaja con esa copia local. Para guardar, insertar o dar de baja, el repositorio abre una conexión solo durante la ejecución del procedimiento almacenado y la cierra al terminar.

## Entrega

Incluye el código del proyecto, `NeptunoDB.sql` y capturas de las pestañas Productos, Categorías, Proveedores, Pedidos y Reporte con la aplicación conectada a la base.
