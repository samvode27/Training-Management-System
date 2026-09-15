import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  isRegisterMode = signal(false);
  isLoading = signal(false);
  error = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  loginForm = this.fb.group({
    username: ['admin', [Validators.required]],
    password: ['Password123!', [Validators.required]],
  });

  registerForm = this.fb.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(12)]],
    role: ['Student', [Validators.required]],
  });

  toggleMode() {
    this.isRegisterMode.update((v) => !v);
    this.error.set(null);
    this.successMessage.set(null);
  }

  async onLogin() {
    if (this.loginForm.invalid) return;

    this.isLoading.set(true);
    this.error.set(null);
    this.successMessage.set(null);

    try {
      const { username, password } = this.loginForm.getRawValue();
      await this.auth.login({ username: username!, password: password! });
      
      const user = this.auth.currentUser();
      if (user?.role === 'Student') {
        this.router.navigate(['/student']);
      } else {
        // Admin and Instructor both land on Dashboard directly
        this.router.navigate(['/dashboard']);
      }
    } catch (err: any) {
      if (err.status === 403 || (err.error?.detail && err.error.detail.includes('pending Administrator'))) {
        this.error.set('Registration Pending Approval: Your account application was received and is awaiting Administrator review. Access will be activated upon approval.');
      } else if (err.status === 403 && err.error?.detail && err.error.detail.includes('declined')) {
        this.error.set('Registration Declined: This account application was declined by the administrator.');
      } else {
        this.error.set(err.error?.detail || err.error?.message || 'Login failed. Please verify your credentials or ensure your account is approved.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  async onRegister() {
    if (this.registerForm.invalid) return;

    this.isLoading.set(true);
    this.error.set(null);
    this.successMessage.set(null);

    try {
      const formVal = this.registerForm.getRawValue();
      await this.auth.register({
        firstName: formVal.firstName!,
        lastName: formVal.lastName!,
        email: formVal.email!,
        password: formVal.password!,
        role: formVal.role!,
      });

      // Switch to sign in tab and display the pending approval notice
      this.isRegisterMode.set(false);
      this.loginForm.patchValue({
        username: formVal.email,
        password: '',
      });
      this.successMessage.set(
        'Registration submitted successfully! Your account is currently Pending Administrator Approval. An administrator will review and activate your account.'
      );
      this.registerForm.reset({ role: 'Student' });
    } catch (err: any) {
      if (err.error?.errors && Array.isArray(err.error.errors)) {
        this.error.set(err.error.errors.join(' '));
      } else {
        this.error.set(err.error?.detail || err.error?.message || 'Registration failed. Please ensure password meets enterprise requirements (min 12 chars, uppercase, number, symbol).');
      }
    } finally {
      this.isLoading.set(false);
    }
  }
}
