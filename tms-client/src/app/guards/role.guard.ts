import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Functional Role Guard requiring a specific role (or Admin).
 * Checks session if not authenticated, redirects to /login if unauthenticated,
 * or /unauthorized if role is insufficient.
 */
export const roleGuard = (requiredRole: string): CanActivateFn => {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      await auth.checkSession();
    }

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (auth.hasRole(requiredRole)) {
      return true;
    }

    return router.createUrlTree(['/unauthorized']);
  };
};

/**
 * Functional Role Guard allowing any of multiple roles.
 * Checks session if not authenticated, redirects to /login if unauthenticated,
 * or /unauthorized if none of the roles match.
 */
export const anyRoleGuard = (allowedRoles: string[]): CanActivateFn => {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      await auth.checkSession();
    }

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (auth.hasAnyRole(allowedRoles)) {
      return true;
    }

    return router.createUrlTree(['/unauthorized']);
  };
};
