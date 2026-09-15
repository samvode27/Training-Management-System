import { Component, inject, OnInit, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { exhaustMap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GradeService, GradePayload } from '../../services/grade.service';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'tms-grade-submission',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './grade-submission.component.html',
  styleUrl: './grade-submission.component.scss',
})
export class GradeSubmissionComponent implements OnInit {
  private api = inject(GradeService);
  private fb = inject(FormBuilder);
  enrollmentStore = inject(EnrollmentStore);
  auth = inject(AuthService);

  // Only Approved students can be evaluated for final grades
  approvedStudents = computed(() => {
    return this.enrollmentStore
      .scopedEnrollments()
      .filter((e) => e.status === 'Approved');
  });

  pendingStudents = computed(() => {
    return this.enrollmentStore
      .scopedEnrollments()
      .filter((e) => e.status === 'Pending');
  });

  gradeForm = this.fb.group({
    studentId: [null as number | null, [Validators.required, Validators.min(1)]],
    courseId: [null as number | null, [Validators.required, Validators.min(1)]],
    score: [85, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  isSubmitting = signal(false);
  submissionStatus = signal('');
  isSuccess = signal(false);

  private submitClick$ = new Subject<GradePayload>();

  constructor() {
    this.submitClick$
      .pipe(
        exhaustMap((payload) => {
          this.isSubmitting.set(true);
          this.submissionStatus.set('Submitting grade to server...');
          this.isSuccess.set(false);
          return this.api.postGrade(payload);
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (result) => {
          this.isSubmitting.set(false);
          this.isSuccess.set(true);
          this.submissionStatus.set(
            `Grade persisted successfully! Record ID: ${result.id}`
          );
          const raw = this.gradeForm.getRawValue();
          if (raw.studentId && raw.courseId && raw.score !== null) {
            this.enrollmentStore.updateGrade(
              Number(raw.studentId),
              Number(raw.courseId),
              Number(raw.score)
            );
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.isSuccess.set(false);
          this.submissionStatus.set(
            `Submission failed: ${err?.error?.detail || err?.error?.title || err?.message || 'Server error'}`
          );
        },
      });

    // Auto-select first approved student in instructor's cohort when loaded
    effect(() => {
      const approved = this.approvedStudents();
      const currentCourseId = this.gradeForm.controls.courseId.value;
      if (approved.length > 0 && !currentCourseId) {
        this.gradeForm.patchValue({
          studentId: approved[0].studentId,
          courseId: approved[0].courseId,
        });
      }
    });
  }

  ngOnInit() {
    this.enrollmentStore.setScope('my');
    this.enrollmentStore.loadEnrollments();
  }

  onSelectEnrollment(enrollmentId: string) {
    const enrollment = this.enrollmentStore
      .scopedEnrollments()
      .find((e) => e.id === enrollmentId);
    if (enrollment) {
      this.gradeForm.patchValue({
        studentId: enrollment.studentId,
        courseId: enrollment.courseId,
      });
    }
  }

  onSubmit() {
    if (!this.gradeForm.valid) {
      this.gradeForm.markAllAsTouched();
      return;
    }

    const rawValue = this.gradeForm.getRawValue();
    const cId = Number(rawValue.courseId);
    const sId = Number(rawValue.studentId);

    const enrollment = this.enrollmentStore
      .entities()
      .find((e) => e.courseId === cId && e.studentId === sId);

    // Business Rule 1: Instructor course assignment guard
    if (!this.auth.hasRole('Admin')) {
      if (enrollment && !this.auth.isCourseOwner(enrollment.courseInstructorId)) {
        this.isSuccess.set(false);
        this.submissionStatus.set(
          'Access Denied: You are not authorized to grade students outside of your assigned courses.'
        );
        return;
      }
    }

    // Business Rule 2: Cannot grade pending or unapproved enrollments
    if (enrollment && enrollment.status !== 'Approved') {
      this.isSuccess.set(false);
      this.submissionStatus.set(
        `Cannot submit grade: Student ${enrollment.studentName}'s enrollment request is currently '${enrollment.status}'. You must approve the enrollment request in the Enrollments page before evaluating grades.`
      );
      return;
    }

    this.submitClick$.next({
      studentId: sId,
      courseId: cId,
      score: Number(rawValue.score),
    });
  }
}
