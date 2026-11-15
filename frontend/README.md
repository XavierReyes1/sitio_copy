# SpendAudit — Frontend (Fase 3)

React + TypeScript + Vite + Tailwind. Arquitectura **Vertical Slicing**: cada funcionalidad agrupa su API, tipos, esquemas Zod y componentes.

## Puesta en marcha

```bash
cp .env.example .env     # VITE_API_BASE_URL=http://localhost:5080/api/v1
pnpm install             # (o npm install)
pnpm dev                 # http://localhost:5173  (el backend ya permite este origen por CORS)
```

Scripts: `dev` · `build` (tsc + vite) · `test` (Vitest) · `preview`.

## Estructura

```
src/
├── core/                     # Transversal (sin lógica de negocio)
│   ├── api/                  # apiClient (Axios + interceptores) y extractor universal de errores
│   ├── auth/                 # roles y almacenamiento de la sesión (JWT)
│   ├── hooks/ · utils/       # useCargaDatos, formato de montos y fechas
│   └── ui/                   # Button, Input/Select/Textarea, Modal, Alert...
├── features/
│   ├── auth/                 # login, registro de empresa, AuthProvider
│   ├── usuarios/             # puestos y usuarios (Admin)
│   ├── politicas/            # política de gastos (Admin edita, Auditor consulta)
│   └── gastos/               # álbum, carga drag & drop, edición, liquidar lote, lotes y semáforo
│       └── {api, components, hooks, schemas, types, utils}
├── layouts/AppLayout.tsx     # menú según rol + <Outlet />
├── router/                   # AppRouter + guardias (RutaProtegida, RutaPublica, RutaPorRol)
└── App.tsx                   # AuthProvider + AppRouter
```

## Cómo cumple la rúbrica

- **Rutas protegidas:** `RutaProtegida` bloquea a quien no tenga JWT (lo manda a `/login` y recuerda a dónde iba); `RutaPorRol` restringe por rol (el backend vuelve a validar con 403).
- **Validación sintáctica en vivo:** Zod + React Hook Form con `mode: 'onChange'`. Cada esquema es **espejo** de su validador de FluentValidation (mismos límites y mensajes). Los botones se habilitan solo con el formulario válido.
- **Resiliencia HTTP:**
  - `apiClient` adjunta `Authorization: Bearer <jwt>` en cada petición.
  - Un 401 con sesión activa cierra la sesión y redirige al login.
  - `extraerMensajeErrorBackend` entiende `[{propertyName, errorMessage}]` (FluentValidation, 400), `{ mensaje }` (401/403/404/409/500), ProblemDetails y "sin respuesta" (API apagada o CORS).

## Decisiones

- **Sesión en `localStorage`** (clave `spendaudit.sesion`) con la fecha de expiración del JWT: al vencer se cierra sola. Es la opción simple del curso; la alternativa más segura (cookie `HttpOnly`) requiere cambios en el backend.
- **Los archivos no son URLs públicas:** «Ver archivo» descarga el blob con el JWT y lo abre en una pestaña nueva.
- **OCR de imágenes:** JPG/PNG se reconocen con Tesseract.js en el navegador; la imagen no se envía a un proveedor de OCR. La primera ejecución descarga el motor y los datos de español, por lo que requiere conexión a internet. Los PDFs con texto seleccionable los extrae el backend; los PDFs escaneados siguen requiriendo OCR.
- **Actualización local:** subir, editar y eliminar actualizan la lista en memoria sin repetir el GET.
- **Montos y fechas como texto** en los formularios y conversión al enviar, para que el esquema Zod sea idéntico a las reglas del backend sin transformaciones.
- **«Hoy» en Honduras (UTC−6)**, igual que `HoraLocal.Hoy()` del backend, para validar fechas futuras.

## Pruebas (`pnpm test`)

72 pruebas: extractor de errores, esquemas Zod (casos espejo de FluentValidation), conversiones, guardias de ruta por rol, login (validación en vivo, éxito y error 401) y el álbum (selección, liquidación, eliminación).

## Despliegue (Fase 5)

`vercel.json` ya reescribe todas las rutas a `index.html` para que `react-router` funcione al recargar. En Vercel define `VITE_API_BASE_URL` con la URL HTTPS de Render y agrega el dominio de Vercel a `Cors__AllowedOrigins` del backend.
