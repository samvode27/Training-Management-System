import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CourseStore } from '../../store/course.store';
import { AuthService } from '../../services/auth.service';
import { AdminUserService } from '../../services/admin-user.service';
import { CurriculumService, CourseCurriculum } from '../../services/curriculum.service';
import { Course } from '../../models/course.model';
import { UserAccount } from '../../models/user-account.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './course-list.component.html',
  styleUrl: './course-list.component.scss',
})
export class CourseListComponent implements OnInit {
  store = inject(CourseStore);
  auth = inject(AuthService);
  private adminUserService = inject(AdminUserService);
  private curriculumService = inject(CurriculumService);
  private http = inject(HttpClient);
  private snackBar = inject(MatSnackBar);

  searchQuery = signal('');
  availabilityFilter = signal<'all' | 'open' | 'full'>('all');
  departmentFilter = signal<string>('all');

  instructorsList = signal<UserAccount[]>([]);

  showCreateModal = signal(false);
  showEditModal = signal(false);
  createModalTab = signal<'basic' | 'curriculum'>('basic');
  editModalTab = signal<'basic' | 'curriculum'>('basic');

  previewCourse = signal<{ course: Course; curriculum: CourseCurriculum } | null>(null);

  isSubmitting = signal(false);
  modalError = signal<string | null>(null);

  // Bookmarked courses for students
  bookmarkedCourseIds = signal<number[]>([]);

  newCourse = {
    code: '',
    title: '',
    maxCapacity: 30,
    instructorId: '',
    department: 'Department of Computer Science & Software Engineering',
    credits: 3.0,
    summary: '',
    description: '',
    prerequisites: 'Standard departmental admission and prerequisite clearance.',
    learningOutcomesText: 'Master core theoretical concepts and domain foundations\nDeliver functional laboratory milestones and assignments\nComplete semester capstone project with automated test suite',
    industrySkillsText: 'Technical Architecture, System Design, Problem Solving, Quality Assurance',
  };

  editCourseData = {
    id: 0,
    code: '',
    title: '',
    maxCapacity: 30,
    instructorId: '',
    department: '',
    credits: 3.0,
    summary: '',
    description: '',
    prerequisites: '',
    learningOutcomesText: '',
    industrySkillsText: '',
  };

  departments = [
    { id: 'all', label: 'All Departments' },
    { id: 'cs', label: 'Computer Science & Software' },
    { id: 'cloud', label: 'DevOps & Cloud Systems' },
    { id: 'web', label: 'Web Science & Frontend' },
    { id: 'data', label: 'Data Science & Databases' },
  ];

  filteredCourses = computed(() => {
    const raw = this.store.scopedCourses();
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.availabilityFilter();
    const dept = this.departmentFilter();

    return raw.filter((c) => {
      const matchesQuery =
        !query ||
        c.title.toLowerCase().includes(query) ||
        c.code.toLowerCase().includes(query) ||
        (c.instructorName && c.instructorName.toLowerCase().includes(query));

      const isFull = (c.enrollmentCount || 0) >= (c.maxCapacity || 1);
      const matchesFilter =
        filter === 'all' ||
        (filter === 'open' && !isFull) ||
        (filter === 'full' && isFull);

      let matchesDept = true;
      if (dept === 'cs') {
        matchesDept = c.code.includes('CS') || c.title.toLowerCase().includes('computer') || c.title.toLowerCase().includes('algorithm');
      } else if (dept === 'cloud') {
        matchesDept = c.code.includes('CLD') || c.title.toLowerCase().includes('cloud') || c.title.toLowerCase().includes('devops') || c.code.includes('INSTRU');
      } else if (dept === 'web') {
        matchesDept = c.code.includes('WEB') || c.code.includes('CSE') || c.title.toLowerCase().includes('web') || c.title.toLowerCase().includes('angular');
      } else if (dept === 'data') {
        matchesDept = c.code.includes('DATA') || c.title.toLowerCase().includes('data') || c.title.toLowerCase().includes('database');
      }

      return matchesQuery && matchesFilter && matchesDept;
    });
  });

  ngOnInit() {
    this.loadBookmarks();

    if (this.auth.hasRole('Admin')) {
      this.store.setScope('all');
      this.loadInstructors();
    } else if (this.auth.hasRole('Instructor')) {
      this.store.setScope('my');
    } else {
      this.store.setScope('all');
    }

    this.store.loadCourses();
  }

  loadBookmarks() {
    try {
      const saved = localStorage.getItem('tms_bookmarked_courses');
      if (saved) {
        this.bookmarkedCourseIds.set(JSON.parse(saved));
      }
    } catch (e) {}
  }

  toggleBookmark(courseId: number, e: Event) {
    e.stopPropagation();
    let current = this.bookmarkedCourseIds();
    if (current.includes(courseId)) {
      current = current.filter((id) => id !== courseId);
      this.snackBar.open('Removed course from saved bookmarks.', 'Close', { duration: 2000 });
    } else {
      current = [...current, courseId];
      this.snackBar.open('Added course to saved bookmarks!', 'Close', { duration: 2000 });
    }
    this.bookmarkedCourseIds.set(current);
    localStorage.setItem('tms_bookmarked_courses', JSON.stringify(current));
  }

  isBookmarked(courseId: number): boolean {
    return this.bookmarkedCourseIds().includes(courseId);
  }

  loadInstructors() {
    this.adminUserService.getUsers().subscribe({
      next: (users) => {
        const instructors = users.filter(
          (u) => (u.role === 'Instructor' || u.role === 'Admin') && u.approvalStatus === 'Approved'
        );
        this.instructorsList.set(instructors);
      },
      error: () => {
        this.instructorsList.set([]);
      },
    });
  }

  isAdmin() {
    return this.auth.hasRole('Admin');
  }

  isInstructor() {
    return this.auth.hasRole('Instructor') && !this.auth.hasRole('Admin');
  }

  isStudent() {
    return this.auth.hasRole('Student');
  }

  isOwner(course: Course): boolean {
    return this.auth.isCourseOwner(course.instructorId);
  }

  setScope(scope: 'all' | 'my') {
    this.store.setScope(scope);
  }

  // Quick Syllabus Preview
  openPreviewModal(course: Course, e: Event) {
    e.preventDefault();
    e.stopPropagation();
    const curr = this.curriculumService.getCurriculum(course.id, course.title, course.code);
    this.previewCourse.set({ course, curriculum: curr });
  }

  closePreviewModal() {
    this.previewCourse.set(null);
  }

  openCreateModal() {
    if (!this.isAdmin()) {
      this.snackBar.open('Restricted: Only Administrators can create new courses.', 'Close', {
        duration: 3500,
      });
      return;
    }
    const defaultInstructor = this.instructorsList()[0]?.id || this.auth.currentUser()?.userId || '';
    this.newCourse = {
      code: '',
      title: '',
      maxCapacity: 30,
      instructorId: defaultInstructor,
      department: 'Department of Computer Science & Software Engineering',
      credits: 3.0,
      summary: '',
      description: '',
      prerequisites: 'Standard departmental admission and prerequisite clearance.',
      learningOutcomesText: 'Master core theoretical concepts and domain foundations\nDeliver functional laboratory milestones and assignments\nComplete semester capstone project with automated test suite',
      industrySkillsText: 'Technical Architecture, System Design, Problem Solving, Quality Assurance',
    };
    this.createModalTab.set('basic');
    this.modalError.set(null);
    this.showCreateModal.set(true);
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
  }

  openEditModal(course: Course) {
    if (!this.isAdmin() && !this.isOwner(course)) {
      this.snackBar.open(`Access Denied: You do not own ${course.code}.`, 'Close', {
        duration: 3500,
      });
      return;
    }

    const currentCurriculum = this.curriculumService.getCurriculum(course.id, course.title, course.code);

    this.editCourseData = {
      id: course.id,
      code: course.code,
      title: course.title,
      maxCapacity: course.maxCapacity,
      instructorId: course.instructorId || '',
      department: currentCurriculum.department,
      credits: currentCurriculum.credits,
      summary: currentCurriculum.summary,
      description: currentCurriculum.description,
      prerequisites: currentCurriculum.prerequisites,
      learningOutcomesText: (currentCurriculum.learningOutcomes || []).join('\n'),
      industrySkillsText: (currentCurriculum.industrySkills || []).join(', '),
    };

    this.editModalTab.set('basic');
    this.modalError.set(null);
    this.showEditModal.set(true);
  }

  closeEditModal() {
    this.showEditModal.set(false);
  }

  submitCreateCourse() {
    if (!this.newCourse.code || !this.newCourse.title) {
      this.modalError.set('Course code and title are required.');
      return;
    }

    this.isSubmitting.set(true);
    this.modalError.set(null);

    const code = this.newCourse.code.toUpperCase().trim();
    const title = this.newCourse.title.trim();
    const instructorId =
      this.newCourse.instructorId ||
      this.auth.currentUser()?.userId ||
      this.auth.currentUser()?.displayName ||
      'admin';

    const outcomes = this.newCourse.learningOutcomesText
      .split('\n')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    const skills = this.newCourse.industrySkillsText
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    const defaultCurr = this.curriculumService.generateDefaultCurriculum(title, code);
    const customCurr: CourseCurriculum = {
      ...defaultCurr,
      summary: this.newCourse.summary.trim() || defaultCurr.summary,
      description: this.newCourse.description.trim() || defaultCurr.description,
      department: this.newCourse.department.trim() || defaultCurr.department,
      credits: Number(this.newCourse.credits) || 3.0,
      prerequisites: this.newCourse.prerequisites.trim() || defaultCurr.prerequisites,
      learningOutcomes: outcomes.length ? outcomes : defaultCurr.learningOutcomes,
      industrySkills: skills.length ? skills : defaultCurr.industrySkills,
    };

    this.store.createCourse(
      {
        code: code,
        title: title,
        maxCapacity: Number(this.newCourse.maxCapacity) || 30,
        instructorId: instructorId,
        department: customCurr.department,
        credits: customCurr.credits,
        summary: customCurr.summary,
        description: customCurr.description,
        prerequisites: customCurr.prerequisites,
        learningOutcomesJson: JSON.stringify(customCurr.learningOutcomes),
        syllabusJson: JSON.stringify(customCurr.modules),
        industrySkillsJson: JSON.stringify(customCurr.industrySkills),
      },
      () => {
        // Cache under code and placeholder ID
        this.curriculumService.saveCurriculum(code, customCurr, code);

        this.isSubmitting.set(false);
        this.showCreateModal.set(false);
        this.snackBar.open(
          `Course ${code} created and saved to database!`,
          'Close',
          { duration: 3500 }
        );
        this.store.loadCourses();
      },
      (error) => {
        this.isSubmitting.set(false);
        this.modalError.set(error);
      }
    );
  }

  submitUpdateCourse() {
    if (!this.editCourseData.title) {
      this.modalError.set('Course title is required.');
      return;
    }

    this.isSubmitting.set(true);
    this.modalError.set(null);

    const outcomes = this.editCourseData.learningOutcomesText
      .split('\n')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    const skills = this.editCourseData.industrySkillsText
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    const defaultCurr = this.curriculumService.generateDefaultCurriculum(
      this.editCourseData.title,
      this.editCourseData.code
    );

    const customCurr: CourseCurriculum = {
      ...defaultCurr,
      summary: this.editCourseData.summary.trim() || defaultCurr.summary,
      description: this.editCourseData.description.trim() || defaultCurr.description,
      department: this.editCourseData.department.trim() || defaultCurr.department,
      credits: Number(this.editCourseData.credits) || 3.0,
      prerequisites: this.editCourseData.prerequisites.trim() || defaultCurr.prerequisites,
      learningOutcomes: outcomes.length ? outcomes : defaultCurr.learningOutcomes,
      industrySkills: skills.length ? skills : defaultCurr.industrySkills,
    };

    const payload: any = {
      title: this.editCourseData.title.trim(),
      code: this.editCourseData.code.trim(),
      maxCapacity: Number(this.editCourseData.maxCapacity) || 30,
      department: customCurr.department,
      credits: customCurr.credits,
      summary: customCurr.summary,
      description: customCurr.description,
      prerequisites: customCurr.prerequisites,
      learningOutcomesJson: JSON.stringify(customCurr.learningOutcomes),
      syllabusJson: JSON.stringify(customCurr.modules),
      industrySkillsJson: JSON.stringify(customCurr.industrySkills),
    };

    if (this.editCourseData.instructorId) {
      payload.instructorId = this.editCourseData.instructorId;
    }

    this.http.put(`${environment.apiUrl}/courses/${this.editCourseData.id}`, payload).subscribe({
      next: () => {
        this.curriculumService.saveCurriculum(this.editCourseData.id, customCurr, this.editCourseData.code);

        this.isSubmitting.set(false);
        this.showEditModal.set(false);
        this.snackBar.open(
          `Course ${this.editCourseData.code} and description saved to database!`,
          'Close',
          { duration: 3500 }
        );
        this.store.loadCourses();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        if (err.status === 403) {
          this.modalError.set(
            'Security Violation: You do not have permission to edit this course (403 Forbidden).'
          );
        } else {
          this.modalError.set(err.error?.detail || err.message || 'Failed to update course');
        }
      },
    });
  }

  onDelete(id: number, title: string, instructorId?: string | null) {
    if (!this.isAdmin() && !this.auth.isCourseOwner(instructorId)) {
      this.snackBar.open(
        `Permission Denied: Only administrators or the course owner can delete this module.`,
        'Close',
        { duration: 4000 }
      );
      return;
    }

    if (confirm(`Are you sure you want to delete course "${title}"?`)) {
      this.store.deleteCourse(id);

      setTimeout(() => {
        const error = this.store.error();
        if (error) {
          this.snackBar.open(error, 'Close', { duration: 5000 });
        }
      }, 300);
    }
  }
}
