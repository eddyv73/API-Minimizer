# API-Minimizer

## Descripción

**Sí, este repositorio contiene APIs.** Este proyecto incluye dos APIs REST desarrolladas con ASP.NET Core 6.0 para demostrar conceptos de minimización y optimización de APIs.

## APIs Incluidas

### 1. ApiFront (API Frontend)
API frontend orientada a la presentación de datos con los siguientes endpoints:

- **EnvsController** (`/api/Envs`)
  - `GET /api/Envs` - Obtiene lista de valores
  - `GET /api/Envs/{id}` - Obtiene un valor específico
  - `POST /api/Envs` - Crea un nuevo registro
  - `PUT /api/Envs/{id}` - Actualiza un registro
  - `DELETE /api/Envs/{id}` - Elimina un registro

- **WeatherForecastController** (`/WeatherForecast`)
  - `GET /WeatherForecast` - Obtiene pronósticos del tiempo de ejemplo

### 2. ApiBack (API Backend)
API backend con lógica de negocio compleja para sistema bancario:

- **TarjetasController** (`/api/Tarjetas`)
  - `GET /api/Tarjetas` - Lista todas las tarjetas
  - `GET /api/Tarjetas/{numeroTarjeta}` - Obtiene una tarjeta específica
  - `POST /api/Tarjetas` - Crea una nueva tarjeta
  - `PUT /api/Tarjetas/{numeroTarjeta}` - Actualiza una tarjeta
  - `DELETE /api/Tarjetas/{numeroTarjeta}` - Elimina una tarjeta
  - `GET /api/Tarjetas/debug/all` - Endpoint de depuración (información del sistema)
  - `GET /api/Tarjetas/export/{filename}` - Exporta datos a archivo

- **StatusController** (`/api/Status`)
  - `GET /api/Status` - Obtiene valores de estado
  - `GET /api/Status/{id}` - Obtiene estado específico
  - `POST /api/Status` - Verifica estado de API con validación compleja
  - `PUT /api/Status/{id}` - Actualiza una transacción
  - `DELETE /api/Status/{id}` - Elimina una transacción
  - `GET /api/Status/time` - Obtiene hora actual
  - `GET /api/Status/times` - Obtiene múltiples zonas horarias
  - `GET /api/Status/timezone` - Obtiene información de zona horaria

- **BankingSystemController** (TransactionProcessorController) (`/api/TransactionProcessor`)
  - `GET /api/TransactionProcessor/analytics/summary` - Análisis de transacciones con métricas
  - `POST /api/TransactionProcessor/batch` - Procesamiento de lote de transacciones
  - `GET /api/TransactionProcessor/rules/patterns` - Detección de patrones mediante ML

## Características

- **Swagger/OpenAPI**: Ambas APIs incluyen documentación Swagger integrada
- **Docker**: Soporte para contenedores con Docker Compose
- **ASP.NET Core 6.0**: Desarrollado con la última tecnología .NET
- **Anotaciones Swagger**: Documentación enriquecida de endpoints

## Requisitos

- .NET 6.0 SDK
- Docker (opcional, para despliegue con contenedores)
- SQL Server (para ApiBack)

## Cómo Ejecutar

### Opción 1: Ejecutar localmente

#### ApiFront
```bash
cd API-Minimizer/ApiFront
dotnet restore
dotnet run
```
La API estará disponible en `https://localhost:5001` (o el puerto configurado)

#### ApiBack
```bash
cd API-Minimizer/ApiBack/API-Minimizer-back
dotnet restore
dotnet run
```
La API estará disponible en `https://localhost:5002` (o el puerto configurado)

### Opción 2: Ejecutar con Docker Compose

```bash
cd API-Minimizer
docker-compose up --build
```

## Documentación Swagger

Una vez ejecutadas las APIs, puede acceder a la documentación Swagger:

- **ApiFront**: `https://localhost:5001/swagger`
- **ApiBack**: `https://localhost:5002/swagger`

## Estructura del Proyecto

```
API-Minimizer/
├── ApiFront/              # API Frontend
│   ├── Controllers/       # Controladores de API
│   ├── Model/            # Modelos de datos
│   └── Program.cs        # Configuración de la aplicación
├── ApiBack/              # API Backend
│   └── API-Minimizer-back/
│       ├── Controllers/  # Controladores de API
│       └── Program.cs    # Configuración de la aplicación
├── MinimizerModel/       # Modelos compartidos
│   └── MinimizerCommon/  # Clases comunes
└── docker-compose.yml    # Configuración de Docker
```

## Archivos Adicionales

- `cleaner.cpp` - Utilidad en C++ para limpieza de datos
- `sp.sql` - Procedimientos almacenados de base de datos
- `view.sql` - Vistas de base de datos

## Licencia

Ver archivo LICENSE para más detalles.