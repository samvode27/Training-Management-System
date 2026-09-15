import { Component, computed, inject, input, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { rxResource } from '@angular/core/rxjs-interop';
import { CourseService } from '../../services/course.service';
import { CourseDetail } from '../../models/course.model';
import { AuthService } from '../../services/auth.service';
import { CurriculumService, CourseCurriculum } from '../../services/curriculum.service';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './course-detail.component.html',
  styleUrl: './course-detail.component.scss',
})
export class CourseDetailComponent {
  auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private courseService = inject(CourseService);
  private curriculumService = inject(CurriculumService);
  private snackBar = inject(MatSnackBar);

  id = input<string>();

  courseId = computed(() => {
    return this.id() || this.route.snapshot.paramMap.get('id') || '1';
  });

  courseResource = rxResource<CourseDetail, unknown>({
    stream: () => this.courseService.getById(this.courseId()),
  });

  curriculumVersion = signal(0);
  isCurriculumModalOpen = signal(false);

  curriculumForm: CourseCurriculum = {
    summary: '',
    description: '',
    department: '',
    prerequisites: '',
    credits: 3.0,
    learningOutcomes: [],
    modules: [],
    industrySkills: [],
    careerOpportunities: [],
  };

  learningOutcomesText = '';
  industrySkillsText = '';
  careerOpportunitiesText = '';

  curriculum = computed<CourseCurriculum>(() => {
    // depend on version signal so edits trigger recompute
    this.curriculumVersion();
    const course = this.courseResource.value();
    const cId = this.courseId();
    if (!course) {
      return this.curriculumService.getCurriculum(cId, 'Academic Course', 'TMS-101');
    }
    return this.curriculumService.getCurriculum(cId, course.title, course.code);
  });

  isAdmin(): boolean {
    return this.auth.hasRole('Admin');
  }

  isInstructor(): boolean {
    return this.auth.hasRole('Instructor');
  }

  isOwner(course: CourseDetail): boolean {
    return this.auth.isCourseOwner(course.instructorId);
  }

  canEditCurriculum(course: CourseDetail): boolean {
    return this.isAdmin() || this.isOwner(course);
  }

  getCapacityPercentage(course: CourseDetail): number {
    const max = course.maxCapacity || 1;
    const count = course.enrollmentCount || 0;
    return Math.min(100, Math.round((count / max) * 100));
  }

  openCurriculumEditor(course: CourseDetail) {
    const current = this.curriculum();
    this.curriculumForm = JSON.parse(JSON.stringify(current));
    this.learningOutcomesText = (this.curriculumForm.learningOutcomes || []).join('\n');
    this.industrySkillsText = (this.curriculumForm.industrySkills || []).join(', ');
    this.careerOpportunitiesText = (this.curriculumForm.careerOpportunities || []).join(', ');
    this.isCurriculumModalOpen.set(true);
  }

  closeCurriculumEditor() {
    this.isCurriculumModalOpen.set(false);
  }

  saveCurriculumChanges(course: CourseDetail) {
    this.curriculumForm.learningOutcomes = this.learningOutcomesText
      .split('\n')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    this.curriculumForm.industrySkills = this.industrySkillsText
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    this.curriculumForm.careerOpportunities = this.careerOpportunitiesText
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    this.curriculumService.saveCurriculum(this.courseId(), this.curriculumForm, course.code);
    this.curriculumVersion.update((v) => v + 1);
    this.isCurriculumModalOpen.set(false);
    this.snackBar.open('Course curriculum and description updated successfully!', 'Close', {
      duration: 3500,
    });
  }
}