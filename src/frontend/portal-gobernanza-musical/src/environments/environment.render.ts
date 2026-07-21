export const environment = {
  production: true,
  // En Render: frontend (portal-gobernanza.onrender.com) y backend (portal-gobernanza-api.onrender.com)
  // son orígenes distintos. Se usa URL absoluta hacia la API.
  apiBaseUrl: 'https://portal-gobernanza-api.onrender.com/api'
  // Alternativa local (comentar la de arriba y descomentar esta si la app combinada corre en Plesk):
  // apiBaseUrl: '/api'
};