# OrderFlow

OrderFlow permite registrar pedidos y consultar su estado. Cada pedido queda `Pending` mientras Inventory intenta reservar el stock. Si todos los productos tienen unidades disponibles, pasa a `Confirmed`; si alguno no tiene suficiente, pasa a `Rejected`.

## Arquitectura y tecnologías

El proyecto tiene tres aplicaciones: **Orders API**, **Inventory Worker** y **Frontend**. Orders recibe y persiste pedidos; Inventory reserva el stock; RabbitMQ comunica ambos servicios. Cada backend tiene capas de API, aplicación, dominio e infraestructura. Orders usa Outbox para guardar el pedido y el evento en la misma operación de base de datos.

| Tecnología | Uso |
| --- | --- |
| .NET 10 y ASP.NET Core | API REST, servicios en segundo plano y SignalR. |
| Entity Framework Core 10, Npgsql y PostgreSQL 17 | Persistencia; cada servicio tiene su propia base de datos. |
| RabbitMQ 4 y RabbitMQ.Client | Eventos entre Orders e Inventory. |
| React 19, Vite 8 y Tailwind CSS 4 | Interfaz web. |
| SignalR 10 | Notificaciones del estado del pedido. |
| Docker Compose y Nginx | Ejecución conjunta y publicación del frontend. |
| xUnit y EF Core InMemory | Pruebas backend. La prueba de inventario usa PostgreSQL real. |

No hay transacciones distribuidas: el estado del pedido y la reserva de inventario se actualizan en momentos distintos. El patrón Outbox permite publicar el evento si RabbitMQ vuelve a estar disponible. La reserva usa transacciones y bloqueos de PostgreSQL.

## Ejecutar el proyecto

Se requiere Docker con Compose. Desde la raíz, crear un archivo `.env` con valores locales:

```dotenv
POSTGRES_PASSWORD=clave_local
RABBITMQ_PASSWORD=otra_clave_local
VITE_API_BASE_URL=http://localhost:8081/api
```

Levantar las aplicaciones, bases de datos y broker:

```sh
docker compose up --build
```

Las API aplican sus migraciones al iniciar. Inventory agrega automáticamente los productos iniciales que aún no existan; no repone el stock de productos ya creados.

| Contenedor | Imagen o imágenes necesarias |
| --- | --- |
| `orders`, `inventory` | `mcr.microsoft.com/dotnet/sdk:10.0` para compilar y `mcr.microsoft.com/dotnet/aspnet:10.0` para ejecutar. |
| `frontend` | `node:24-alpine` para compilar y `nginx:alpine` para servir el sitio. |
| `orders-db`, `inventory-db` | `postgres:17`. |
| `rabbitmq` | `rabbitmq:4-management`. |
| `backend-tests` | `mcr.microsoft.com/dotnet/sdk:10.0`; se ejecuta con el perfil `test`. |

Compose construye las imágenes de las aplicaciones a partir de sus Dockerfiles. Los volúmenes persistentes son:

| Volumen | Contenedor | Ruta |
| --- | --- | --- |
| `orders-data` | `orders-db` | `/var/lib/postgresql/data` |
| `inventory-data` | `inventory-db` | `/var/lib/postgresql/data` |
| `rabbitmq-data` | `rabbitmq` | `/var/lib/rabbitmq` |

`docker compose down` conserva los datos. `docker compose down -v` también elimina los volúmenes.

Productos iniciales: `SKU-001` (100 unidades, precio 25000), `SKU-002` (100, 18000) y `SKU-003` (100, 22000). La cantidad por SKU no puede superar 100.

## Flujo de un pedido

1. El formulario valida los datos y envía el pedido a Orders. Los errores se muestran en la página.
2. Orders valida el nombre, agrupa SKU repetidos y normaliza sus nombres. Consulta a Inventory para confirmar que existan. Si la validación pasa, guarda el pedido como `Pending` y su evento en Outbox; si un SKU no existe, responde `409`; si Inventory no está disponible, responde `503`.
3. `OutboxPublisher` revisa cada cinco segundos los eventos pendientes y publica `order.created` en el exchange durable `orderflow.events`. Si RabbitMQ falla, el mensaje queda en Outbox y se vuelve a intentar en el siguiente ciclo.
4. Inventory consume el evento, bloquea el pedido y los productos en una transacción, y reserva todas las unidades o ninguna. Guarda el resultado en `ProcessedOrders`.
5. Si recibe de nuevo el mismo pedido, reutiliza el resultado guardado y no descuenta stock por segunda vez.
6. Inventory publica `stock.reserved` con los precios, o `stock.rejected` con el motivo.
7. Orders consume el resultado y cambia el estado solo si el pedido sigue `Pending`. Al confirmar también guarda los precios unitarios.
8. Orders envía `OrderStatusChanged` por SignalR. El frontend se suscribe a los pedidos pendientes y actualiza su estado.

### Comportamiento ante fallos

Si Inventory deja de procesar eventos, el pedido puede quedarse `Pending`: aún no hay vencimiento automático. Si falla el procesamiento, los consumidores vuelven a encolar el mensaje sin límite de intentos y no existe una cola de mensajes fallidos (DLQ). El Outbox también reintenta la publicación sin un máximo configurado. SignalR no guarda ni reproduce notificaciones perdidas. Los planes para completar estos puntos están en [Pendientes y planes](#pendientes-y-planes).

## API y datos que usa el frontend

La URL base local es `http://localhost:8081/api`.

| Método y ruta | Resultado |
| --- | --- |
| `POST /api/orders` | `201 Created` con el identificador y la cabecera `Location`. |
| `GET /api/orders` | `200 OK` con la lista ordenada por fecha descendente. |
| `GET /api/orders/{id}` | `200 OK` con el pedido o `404 Not Found`. |

El frontend solicita `?page=1&pageSize=20`, pero la API aún no aplica paginación.

Entrada para crear un pedido:

```json
{
  "clientName": "María Pérez",
  "items": [
    { "sku": "SKU-001", "quantity": 2 }
  ]
}
```

Respuesta `201 Created`:

```json
{ "id": "f6243135-b93d-47ad-9afd-b309feea8104" }
```

Ejemplo de pedido devuelto por `GET /api/orders`:

```json
[
  {
    "id": "f6243135-b93d-47ad-9afd-b309feea8104",
    "clientName": "María Pérez",
    "createdAt": "2026-10-21T15:00:00+00:00",
    "status": "Confirmed",
    "items": [
      {
        "id": "9a7c081b-a5eb-4e14-b7e1-dd1915deae45",
        "sku": "SKU-001",
        "quantity": 2,
        "unitPrice": 25000
      }
    ]
  }
]
```

Los estados posibles son `Pending`, `Confirmed` y `Rejected`. El precio unitario queda en cero hasta que Inventory confirme la reserva.

El servicio Inventory también ofrece una consulta interna para validar que existan todos los SKU:

```http
POST http://localhost:8082/api/inventory/validate-skus
Content-Type: application/json
```

```json
{ "skus": ["SKU-001", "SKU-002"] }
```

La respuesta es el booleano JSON `true` o `false`; esta consulta no reserva stock.

### Validaciones y errores

El nombre es obligatorio y admite hasta 200 caracteres. Se requiere al menos un artículo, un SKU no vacío y una cantidad entre 1 y 100. El total agrupado por SKU tampoco puede exceder 100.

Si un SKU no existe, el servicio responde `409 Conflict`:

```json
{
  "code": "INVALID_SKUS",
  "message": "La petición contiene SKU no válidos"
}
```

Si la validación no puede completarse porque Inventory no responde, devuelve `503 Service Unavailable` con un cuerpo Problem Details. Los errores de datos inválidos devuelven `400 Bad Request`. El frontend muestra `detail`, `message` o `title`; todavía no presenta los detalles del campo incluidos en `errors`.

### SignalR

Hub: `http://localhost:8081/hubs/orders`. El cliente invoca `SubscribeToOrder(orderId)` y `UnsubscribeFromOrder(orderId)`. El evento `OrderStatusChanged` envía:

```json
{
  "orderId": "f6243135-b93d-47ad-9afd-b309feea8104",
  "status": "Confirmed"
}
```

## Pruebas

Hay cinco pruebas backend:

| Prueba | Casos |
| --- | --- |
| `OrderValidationTests` (1) | Valida nombre, lista de artículos, SKU y cantidades. |
| `OrdersEndpointTests` (1) | Comprueba que un SKU inexistente devuelve `409` sin guardar la orden ni el Outbox. |
| `OrderRepositoryTests` (2) | Confirma una orden una sola vez y evita confirmar una orden rechazada. |
| `InventoryIdempotencyTests` (1) | Reprocesa una orden y comprueba que no se descuente stock dos veces. |

Las cuatro primeras usan EF Core InMemory o un servidor HTTP local. La prueba de idempotencia requiere PostgreSQL para comprobar la lógica de inventario con transacciones y bloqueos.

Ejecutar todas las pruebas:

```sh
docker compose --profile test run --build --rm backend-tests
```

**Importante:** hoy `backend-tests` apunta a la base compartida `InventoryDb` del contenedor `inventory-db`. La prueba aplica migraciones y crea y elimina registros allí. No debe ejecutarse contra producción ni contra una base con datos que deban preservarse.

Para aislarla, agregar al perfil `test` un servicio `inventory-test-db` basado en `postgres:17`, con `POSTGRES_DB=InventoryTestDb`, credenciales por entorno, `healthcheck` y almacenamiento independiente. La imagen de PostgreSQL crea esa base al inicializar el contenedor. Hacer que `backend-tests` espere a que esté lista y actualizar `TEST_INVENTORY_DATABASE` y la guarda del test para aceptar solo esa base. El test debe aplicar las migraciones, usar identificadores y SKU únicos y limpiar sus datos incluso si falla una aserción. Luego, ejecutar varias veces el mismo comando y comprobar que no modifica `InventoryDb`.

## Pendientes y planes

### Vencer pedidos pendientes

Los pedidos que Inventory no procesa pueden permanecer en `Pending` indefinidamente.

Plan: definir cuánto tiempo puede estar pendiente un pedido y configurar ese plazo por entorno. Agregar el estado `Expired` y un proceso periódico en Orders que venza las órdenes que superen el plazo. Las respuestas tardías de Inventory no deben cambiar órdenes vencidas; si el stock ya se reservó, también hay que definir cómo liberarlo. Agregar pruebas para el vencimiento y los mensajes tardíos.

### Limitar reintentos y agregar una DLQ

Hoy los consumidores reencolan indefinidamente los mensajes que fallan. No hay DLQ, por lo que un mensaje problemático puede volver a procesarse sin límite. El Outbox tampoco limita los intentos de publicación.

Plan: definir la cantidad máxima de reintentos y una espera entre ellos. Configurar un Dead Letter Exchange y una DLQ para las colas de Orders e Inventory, conservando el motivo del error y los intentos realizados. Definir qué hacer con los mensajes del Outbox que agoten sus intentos. Documentar cómo revisar y reprocesar los mensajes fallidos, y agregar pruebas para los reintentos y el envío a la DLQ.

### Compensar fallos con una Saga

El flujo actual usa eventos, pero no implementa una Saga ni acciones de compensación. Por ejemplo, Inventory puede reservar stock y luego Orders podría fallar al guardar la confirmación.

Plan: modelar cada pedido como una Saga con pasos persistidos, desde la creación hasta la confirmación o el rechazo. Si un paso falla de forma definitiva, publicar una acción compensatoria —por ejemplo, liberar el stock reservado— y hacer que Inventory la procese de forma idempotente. Registrar el estado y los errores de cada paso para permitir reintentos y seguimiento. Agregar pruebas de fallos entre pasos y de que la compensación no libere stock más de una vez.

### Recuperar cambios tras una desconexión de SignalR

SignalR no guarda ni vuelve a enviar las notificaciones que el cliente pierde mientras está desconectado.

Plan: al reconectar, consultar nuevamente las órdenes para sincronizar sus estados y precios con la API y luego reanudar las suscripciones. Agregar una prueba que simule la desconexión y verifique que el listado quede actualizado.

### Enviar el precio unitario por SignalR

El precio se guarda en Orders al confirmar, pero el evento de SignalR solo envía el estado; el frontend no actualiza el precio hasta volver a consultar.

Plan: incluir los artículos y sus precios en el aviso; actualizar también los datos del pedido en React; agregar pruebas del contrato y de la actualización de la interfaz. Al reconectar, consultar nuevamente la orden para recuperar cambios perdidos.

### Agregar un gateway con YARP

El gateway quedó fuera del alcance de la prueba. Actualmente el navegador llama directamente a Orders.

Plan: crear un servicio ASP.NET Core con `Yarp.ReverseProxy`, enrutar `/api/orders` y `/hubs/orders` hacia Orders, y conservar la negociación y los WebSockets de SignalR. Agregar el servicio a Compose, apuntar el frontend al gateway y probar creación, consulta y notificaciones a través del proxy.

## Acceso y consultas

Con Compose en ejecución, abrir el panel en **http://localhost:5173**.

Últimos pedidos procesados por Inventory:

```sh
docker compose exec inventory-db psql -U postgres -d InventoryDb -c 'SELECT "OrderId", "ProcessedAt", "Result" FROM "ProcessedOrders" ORDER BY "ProcessedAt" DESC LIMIT 10;'
```

Últimas órdenes guardadas por Orders:

```sh
docker compose exec orders-db psql -U postgres -d OrdersDb -c 'SELECT "Id", "ClientName", "Status", "CreatedAt" FROM "Orders" ORDER BY "CreatedAt" DESC LIMIT 10;'
```
