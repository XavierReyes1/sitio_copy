import { AuthProvider } from './features/auth/context/AuthProvider'
import { AppRouter } from './router/AppRouter'

// Orquestador limpio (simetría con Program.cs): sin lógica de negocio.
function App() {
  return (
    <AuthProvider>
      <AppRouter />
    </AuthProvider>
  )
}

export default App
