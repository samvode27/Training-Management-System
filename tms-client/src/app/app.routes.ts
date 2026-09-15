import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard, anyRoleGuard } from './guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then(
        (m) => m.LoginComponent
      ),
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/unauthorized/unauthorized.component').then(
        (m) => m.UnauthorizedComponent
      ),
  },
  {
    path: 'admin/users',
    loadComponent: () =>
      import('./features/admin-users/admin-users.component').then(
        (m) => m.AdminUsersComponent
      ),
    canActivate: [roleGuard('Admin')],
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/instructor-dashboard/instructor-dashboard.component').then(
        (m) => m.InstructorDashboardComponent
      ),
    canActivate: [anyRoleGuard(['Admin', 'Instructor'])],
  },
  {
    path: 'admin/courses',
    loadComponent: () =>
      import('./features/course-list/course-list.component').then(
        (m) => m.CourseListComponent
      ),
    canActivate: [roleGuard('Admin')],
  },
  {
    path: 'instructor/courses',
    loadComponent: () =>
      import('./features/course-list/course-list.component').then(
        (m) => m.CourseListComponent
      ),
    canActivate: [roleGuard('Instructor')],
  },
  {
    path: 'student',
    loadComponent: () =>
      import('./features/student-dashboard/student-dashboard.component').then(
        (m) => m.StudentDashboardComponent
      ),
    canActivate: [anyRoleGuard(['Admin', 'Student'])],
  },
  {
    path: 'enrollments',
    loadComponent: () =>
      import('./features/enrollment-list/enrollment-list.component').then(
        (m) => m.EnrollmentListComponent
      ),
    canActivate: [anyRoleGuard(['Admin', 'Instructor'])],
  },
  {
    path: 'courses',
    loadComponent: () =>
      import('./features/course-list/course-list.component').then(
        (m) => m.CourseListComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'courses/:id',
    loadComponent: () =>
      import('./features/course-detail/course-detail.component').then(
        (m) => m.CourseDetailComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'enroll',
    loadComponent: () =>
      import('./features/enrollment-form/enrollment-form.component').then(
        (m) => m.EnrollmentFormComponent
      ),
    canActivate: [anyRoleGuard(['Admin', 'Student'])],
  },
  {
    path: 'grade-submission',
    loadComponent: () =>
      import('./features/grade-submission/grade-submission.component').then(
        (m) => m.GradeSubmissionComponent
      ),
    canActivate: [roleGuard('Instructor')],
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];