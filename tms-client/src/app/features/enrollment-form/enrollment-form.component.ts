import { Component, inject, OnInit, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EnrollmentStore } from '../../store/enrollment.store';
import { CourseStore } from '../../store/course.store';
import { AuthService } from '../../services/auth.service';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'app-enrollment-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './enrollment-form.component.html',
  styleUrl: './enrollment-form.component.scss',
})
export class EnrollmentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private store = inject(EnrollmentStore);
  private courseStore = inject(CourseStore);
  private auth = inject(AuthService);

  submitted = signal(false);
  createdEnrollment = signal<Enrollment | null>(null);

  courses = this.courseStore.entities;
  currentUser = this.auth.currentUser;

  form = this.fb.nonNullable.group({
    studentId: [
      'STU-1001',
      [
        Validators.required,
        Validators.pattern('^STU-[0-9]{4}$'), // Format: STU-1234
      ],
    ],
    courseId: ['', Validators.required],
    term: ['Fall 2026', Validators.required],
    notes: [''],
    backupCourses: this.fb.array<FormControl<string>>([]),
  });

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      if (params['courseId']) {
        this.form.controls.courseId.setValue(params['courseId'].toString());
      }
    });

    if (this.courseStore.entities().length === 0) {
      this.courseStore.loadCourses();
    }
  }

  get backups() {
    return this.form.controls.backupCourses;
  }

  addBackup() {
    this.backups.push(
      this.fb.control('', {
        nonNullable: true,
        validators: Validators.required,
      })
    );
  }

  removeBackup(index: number) {
    this.backups.removeAt(index);
  }

  submit() {
    if (this.form.valid) {
      const payload = this.form.getRawValue();
      const courseIdNum = Number(payload.courseId);
      const course = this.courseStore.entities().find((c) => c.id === courseIdNum);
      const courseName = course ? `${course.code} - ${course.title}` : `Course #${courseIdNum}`;
      const currentUser = this.auth.currentUser();
      const studentName = currentUser?.displayName || 'Student User';

      const backupList = payload.backupCourses.filter((b) => !!b && b.trim().length > 0);

      const localEnrollment: Enrollment = {
        id: 'ENR-' + Date.now().toString().slice(-6),
        studentId: Number(payload.studentId.replace('STU-', '')) || 101,
        studentName: studentName,
        courseId: courseIdNum,
        courseName: courseName,
        status: 'Pending',
        enrolledAt: new Date().toISOString(),
        notes: payload.notes || undefined,
        backupCourses: backupList.length > 0 ? backupList : undefined,
      };

      // Call database API endpoint asynchronously to persist in PostgreSQL and broadcast to SignalR
      this.store.addEnrollmentAsync({
        studentId: payload.studentId,
        studentName: studentName,
        courseId: courseIdNum,
        term: payload.term,
        notes: payload.notes,
        backupCourses: backupList,
      }).subscribe({
        next: (created) => {
          this.createdEnrollment.set(created || localEnrollment);
          this.submitted.set(true);
        },
        error: () => {
          // Fallback to local store if backend unreachable
          this.store.addEnrollment(localEnrollment);
          this.createdEnrollment.set(localEnrollment);
          this.submitted.set(true);
        }
      });
    } else {
      this.form.markAllAsTouched();
    }
  }
}