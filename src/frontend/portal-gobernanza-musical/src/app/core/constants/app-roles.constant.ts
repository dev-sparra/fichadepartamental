export const AppRoles = {
  Administrador: 'Administrador',
  LiderGobernanza: 'Líder de Gobernanza',
  GestorDepartamental: 'Gestor Departamental'
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];
