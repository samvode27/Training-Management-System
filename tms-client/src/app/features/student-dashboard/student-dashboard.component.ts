import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { Course } from '../../models/course.model';
import { CourseService } from '../../services/course.service';
import { AuthService } from '../../services/auth.service';
import { EnrollmentStore } from '../../store/enrollment.store';
import { CurriculumService, CourseCurriculum } from '../../services/curriculum.service';
import { AdminUserService } from '../../services/admin-user.service';
import { UserAccount } from '../../models/user-account.model';
import { StudentGoalService, StudentGoal } from '../../services/student-goal.service';

export interface StudentSummary {
  id: number | string;
  name: string;
  email: string;
  coursesCount: number;
  gradedCount: number;
  totalScore: number;
  approvedCount: number;
  gpa: string;
  avgScore: string;
}

export interface TrackCourseMilestone {
  code: string;
  name: string;
  department: string;
  credits: number;
  description: string;
  isCore: boolean;
  matchedCourseId?: number | string;
  status: 'completed' | 'in-progress' | 'pending' | 'available';
  statusLabel: string;
  grade?: number;
  isCompleted: boolean;
  isEnrolled: boolean;
}

export interface ProgramTrack {
  id: 'software-eng' | 'cloud-devops' | 'data-ai' | 'all-catalog';
  title: string;
  degreeName: string;
  badge: string;
  description: string;
  courses: {
    code: string;
    name: string;
    department: string;
    credits: number;
    description: string;
    isCore: boolean;
  }[];
}

export interface DegreeRequirement {
  code: string;
  name: string;
  category?: string;
  credits: number;
  completed: boolean;
  grade?: number;
}

export interface AcademicGoal {
  id: string;
  title: string;
  targetTerm: string;
  credits: number;
  completed: boolean;
  grade?: number;
}

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent implements OnInit {
  readonly Math = Math;
  private api = inject(CourseService);
  auth = inject(AuthService);
  private router = inject(Router);
  enrollmentStore = inject(EnrollmentStore);
  private curriculumService = inject(CurriculumService);
  private adminUserService = inject(AdminUserService);
  private goalService = inject(StudentGoalService);

  studentName = computed(() => this.auth.currentUser()?.displayName || 'Student');

  showTranscriptModal = signal(false);
  transcriptStep = signal<'select' | 'view'>('select');
  selectedTranscriptStudent = signal<string | null>(null);
  transcriptStudentSearch = signal('');

  showAnalyticsModal = signal(false);
  showRegistryModal = signal(false);
  showFacultyModal = signal(false);
  showGpaCalculator = signal(false);

  registrySearchQuery = signal('');
  registryStatusFilter = signal<'all' | 'Approved' | 'Pending' | 'Rejected' | 'Graded'>('all');

  activeCertificate = signal<{ student: string; course: string; code: string; date: string } | null>(null);
  previewCourseModal = signal<{ course: Course; curriculum: CourseCurriculum } | null>(null);

  searchQuery = signal('');
  adminViewMode = signal<'cards' | 'distribution' | 'table'>('cards');

  // GPA Target Calculator State
  targetGpa = signal<number>(3.85);
  remainingCredits = computed(() => Math.max(0, 120 - this.earnedCredits()));

  // Personal Academic Focus & Interactive Goals
  studentAcademicGoal = signal('Master Cloud Microservices, Web APIs and Graduate with High Honors standing.');
  isEditingGoal = signal(false);
  newGoalText = signal('');
  studentGoals = signal<AcademicGoal[]>([
    { id: '1', title: 'Complete Web API Architecture with 90%+ score', targetTerm: 'Fall 2026', credits: 3, completed: true, grade: 94 },
    { id: '2', title: 'Deploy Distributed Microservices Capstone to Kubernetes cluster', targetTerm: 'Spring 2027', credits: 4, completed: false },
    { id: '3', title: 'Achieve Cumulative GPA >= 3.85 for Dean\'s High Honor List', targetTerm: 'Graduation', credits: 0, completed: false },
  ]);

  // Active Program Track & Degree Pathways
  selectedTrackId = signal<'software-eng' | 'cloud-devops' | 'data-ai' | 'all-catalog'>('software-eng');

  programTracks: ProgramTrack[] = [
    {
      id: 'software-eng',
      title: 'Software Engineering (B.S.)',
      degreeName: 'Bachelor of Science in Software Engineering',
      badge: 'Core Program Track',
      description: 'Comprehensive software engineering curriculum covering fundamentals, REST APIs, enterprise systems, and capstone defense.',
      courses: [
        { code: 'CS-101', name: 'Intro to Computer Science & Algorithms', department: 'Computer Science', credits: 3, description: 'Foundational computation, complexity analysis, and algorithms.', isCore: true },
        { code: 'CS-201', name: 'Data Structures & System Foundations', department: 'Computer Science', credits: 4, description: 'Abstract data types, trees, graphs, and memory management.', isCore: true },
        { code: 'CSE-203', name: 'Building RESTful Web APIs with ASP.NET Core', department: 'Software Engineering', credits: 3, description: 'REST architecture, token authentication, and API contract design.', isCore: true },
        { code: 'CSE-301', name: 'Advanced Web API & Enterprise Security', department: 'Software Engineering', credits: 3, description: 'Resource-based auth, rate limiting, and distributed caching.', isCore: true },
        { code: 'DEV-401', name: 'Cloud Native Microservices & Kubernetes', department: 'Cloud & DevOps', credits: 4, description: 'Container orchestration, service mesh, and CI/CD pipelines.', isCore: false },
        { code: 'CAP-400', name: 'Senior Software Capstone & Defense', department: 'Software Engineering', credits: 4, description: 'Full-lifecycle project design, implementation, and committee defense.', isCore: true },
      ],
    },
    {
      id: 'cloud-devops',
      title: 'Cloud Architecture & DevOps',
      degreeName: 'Cloud Systems Specialization Track',
      badge: 'Cloud Engineering',
      description: 'Specialized infrastructure engineering covering containerization, Kubernetes, distributed systems, and security.',
      courses: [
        { code: 'CS-101', name: 'Intro to Computer Science & Algorithms', department: 'Computer Science', credits: 3, description: 'Core principles of software computing.', isCore: true },
        { code: 'DEV-401', name: 'Cloud Native Microservices & Kubernetes', department: 'Cloud & DevOps', credits: 4, description: 'Containerization, ingress controllers, and auto-scaling.', isCore: true },
        { code: 'CSE-203', name: 'Building RESTful Web APIs with ASP.NET Core', department: 'Software Engineering', credits: 3, description: 'Backend service communication and endpoints.', isCore: true },
        { code: 'SEC-301', name: 'Enterprise Infrastructure & Network Defense', department: 'Cyber Security', credits: 3, description: 'Zero-trust architecture, TLS, and security compliance.', isCore: true },
        { code: 'CAP-400', name: 'Senior Software Capstone & Defense', department: 'Software Engineering', credits: 4, description: 'Cloud infrastructure automated deployment capstone.', isCore: true },
      ],
    },
    {
      id: 'data-ai',
      title: 'Data Systems & Backend Architecture',
      degreeName: 'Data Architecture Specialization Track',
      badge: 'Data Systems',
      description: 'Relational & distributed database systems, SQL query optimization, and high-throughput data pipelines.',
      courses: [
        { code: 'CS-101', name: 'Intro to Computer Science & Algorithms', department: 'Computer Science', credits: 3, description: 'Foundational computation and algorithms.', isCore: true },
        { code: 'CS-201', name: 'Data Structures & System Foundations', department: 'Computer Science', credits: 4, description: 'Trees, hash tables, and search strategies.', isCore: true },
        { code: 'DATA-201', name: 'Database Design & SQL Optimization', department: 'Data Systems', credits: 3, description: 'Schema normalization, indexing, and transactional isolation.', isCore: true },
        { code: 'DATA-301', name: 'Distributed Data Pipelines & Stream Processing', department: 'Data Systems', credits: 4, description: 'Kafka streaming, message brokers, and event pipelines.', isCore: true },
        { code: 'CAP-400', name: 'Senior Software Capstone & Defense', department: 'Software Engineering', credits: 4, description: 'Data-intensive backend capstone evaluation.', isCore: true },
      ],
    },
  ];

  instructorsList = signal<UserAccount[]>([]);

  // Active track details with dynamic matching against student's real enrollments and system catalog
  activeTrackMilestones = computed(() => {
    const trackId = this.selectedTrackId();
    const enrollments = this.myEnrollments();
    const catalog = this.coursesResource.value() || [];

    if (trackId === 'all-catalog') {
      return catalog.map((c) => {
        const match = enrollments.find(
          (e) =>
            (e.courseId && String(e.courseId) === String(c.id)) ||
            (e.courseName && (e.courseName.toLowerCase().includes(c.title.toLowerCase()) || c.title.toLowerCase().includes(e.courseName.toLowerCase())))
        );

        const isApproved = !!match && match.status === 'Approved';
        const isPending = !!match && match.status === 'Pending';
        const isCompleted = isApproved && match?.grade !== undefined && match?.grade !== null;

        let status: 'completed' | 'in-progress' | 'pending' | 'available' = 'available';
        let statusLabel = 'Available to Enroll';

        if (isCompleted) {
          status = 'completed';
          statusLabel = `Completed (${match.grade}%)`;
        } else if (isApproved) {
          status = 'in-progress';
          statusLabel = 'Active Enrollment';
        } else if (isPending) {
          status = 'pending';
          statusLabel = 'Pending Approval';
        }

        return {
          code: c.code || 'CRS',
          name: c.title,
          department: 'Academic Curriculum',
          credits: 3,
          description: `Academy course offering led by faculty. Max capacity: ${c.maxCapacity || 30}.`,
          isCore: true,
          matchedCourseId: c.id,
          status,
          statusLabel,
          grade: match?.grade ?? undefined,
          isCompleted,
          isEnrolled: !!match,
        } as TrackCourseMilestone;
      });
    }

    const currentTrack = this.programTracks.find((t) => t.id === trackId) || this.programTracks[0];

    return currentTrack.courses.map((req) => {
      const match = enrollments.find((e) => {
        const eName = (e.courseName || '').toLowerCase();
        const reqName = req.name.toLowerCase();
        const reqCode = req.code.toLowerCase().replace('-', '');
        return eName.includes(reqCode) || eName.includes(reqName.split(' ')[0].toLowerCase()) || reqName.includes(eName);
      });

      const catalogMatch = catalog.find(
        (c) =>
          (c.code && c.code.toLowerCase().replace('-', '') === req.code.toLowerCase().replace('-', '')) ||
          (c.title && (c.title.toLowerCase().includes(req.name.toLowerCase()) || req.name.toLowerCase().includes(c.title.toLowerCase())))
      );

      const isApproved = !!match && match.status === 'Approved';
      const isPending = !!match && match.status === 'Pending';
      const isCompleted = isApproved && match?.grade !== undefined && match?.grade !== null;

      let status: 'completed' | 'in-progress' | 'pending' | 'available' = 'available';
      let statusLabel = 'Available to Enroll';

      if (isCompleted) {
        status = 'completed';
        statusLabel = `Completed (${match.grade}%)`;
      } else if (isApproved) {
        status = 'in-progress';
        statusLabel = 'Active Enrollment';
      } else if (isPending) {
        status = 'pending';
        statusLabel = 'Pending Approval';
      }

      return {
        code: req.code,
        name: req.name,
        department: req.department,
        credits: req.credits,
        description: req.description,
        isCore: req.isCore,
        matchedCourseId: catalogMatch?.id || match?.courseId,
        status,
        statusLabel,
        grade: match?.grade ?? undefined,
        isCompleted,
        isEnrolled: !!match,
      } as TrackCourseMilestone;
    });
  });

  // Track Summary Statistics
  trackStats = computed(() => {
    const milestones = this.activeTrackMilestones();
    const totalCredits = milestones.reduce((acc, m) => acc + m.credits, 0) || 1;
    const completedCredits = milestones.filter((m) => m.isCompleted).reduce((acc, m) => acc + m.credits, 0);
    const inProgressCredits = milestones.filter((m) => m.status === 'in-progress').reduce((acc, m) => acc + m.credits, 0);
    const progressPercent = Math.min(100, Math.round(((completedCredits + inProgressCredits * 0.5) / totalCredits) * 100));
    const completedCount = milestones.filter((m) => m.isCompleted).length;
    const totalCount = milestones.length;

    return {
      totalCredits,
      completedCredits,
      inProgressCredits,
      progressPercent,
      completedCount,
      totalCount,
      isGraduationReady: completedCredits >= totalCredits * 0.85,
    };
  });

  // Academic standing badge based on GPA
  academicHonorStanding = computed(() => {
    const gpa = parseFloat(this.cumulativeGpa());
    if (gpa >= 3.9) return { title: 'Summa Cum Laude / Dean\'s Highest Honors', badgeClass: 'honor-gold' };
    if (gpa >= 3.75) return { title: 'Magna Cum Laude / Dean\'s Honor List', badgeClass: 'honor-purple' };
    if (gpa >= 3.5) return { title: 'Cum Laude / Academic Distinction', badgeClass: 'honor-blue' };
    return { title: 'Good Academic Standing', badgeClass: 'honor-green' };
  });

  // Goal Planner Methods
  addAcademicGoal() {
    const text = this.newGoalText().trim();
    if (!text) return;

    const studentId = this.auth.currentUser()?.displayName || 'Student';
    this.goalService
      .createGoal({
        studentId,
        title: text,
        targetTerm: 'Active Term',
        targetCredits: 3,
      })
      .subscribe({
        next: (created) => {
          const newGoal: AcademicGoal = {
            id: String(created.id),
            title: created.title,
            targetTerm: created.targetTerm,
            credits: Number(created.targetCredits),
            completed: created.isCompleted,
          };
          this.studentGoals.update((goals) => [newGoal, ...goals]);
        },
        error: () => {
          const newGoal: AcademicGoal = {
            id: Date.now().toString(),
            title: text,
            targetTerm: 'Active Term',
            credits: 3,
            completed: false,
          };
          this.studentGoals.update((goals) => [newGoal, ...goals]);
        },
      });

    this.newGoalText.set('');
  }

  toggleGoalCompletion(goalId: string) {
    const current = this.studentGoals().find((g) => g.id === goalId);
    const newCompleted = current ? !current.completed : true;

    this.studentGoals.update((goals) =>
      goals.map((g) => (g.id === goalId ? { ...g, completed: newCompleted } : g))
    );

    const parsedId = Number(goalId);
    if (!isNaN(parsedId)) {
      this.goalService.updateGoal(parsedId, { isCompleted: newCompleted }).subscribe();
    }
  }

  deleteGoal(goalId: string) {
    this.studentGoals.update((goals) => goals.filter((g) => g.id !== goalId));

    const parsedId = Number(goalId);
    if (!isNaN(parsedId)) {
      this.goalService.deleteGoal(parsedId).subscribe();
    }
  }

  selectTrack(trackId: 'software-eng' | 'cloud-devops' | 'data-ai' | 'all-catalog') {
    this.selectedTrackId.set(trackId);
  }

  handleMilestoneAction(milestone: TrackCourseMilestone) {
    if (milestone.matchedCourseId) {
      this.router.navigate(['/courses', milestone.matchedCourseId]);
    } else {
      this.router.navigate(['/courses']);
    }
  }

  // Compatibility signals for any existing bindings
  degreeRequirements = computed<DegreeRequirement[]>(() => {
    return this.activeTrackMilestones().map((m) => ({
      code: m.code,
      name: m.name,
      category: m.department,
      credits: m.credits,
      completed: m.isCompleted,
      grade: m.grade,
    }));
  });

  completedDegreeCredits = computed(() => this.trackStats().completedCredits);

  ngOnInit() {
    this.enrollmentStore.loadEnrollments();
    this.loadGoals();
    if (this.isAdmin()) {
      this.loadInstructors();
    }
  }

  loadGoals() {
    const studentId = this.auth.currentUser()?.displayName || 'Student';
    this.goalService.getGoals(studentId).subscribe({
      next: (goals) => {
        if (goals && goals.length > 0) {
          this.studentGoals.set(
            goals.map((g) => ({
              id: String(g.id),
              title: g.title,
              targetTerm: g.targetTerm,
              credits: Number(g.targetCredits),
              completed: g.isCompleted,
            }))
          );
        }
      },
      error: () => {},
    });
  }

  loadInstructors() {
    this.adminUserService.getUsers().subscribe({
      next: (users) => {
        const instructors = users.filter(
          (u) => (u.role === 'Instructor' || u.role === 'Admin') && u.approvalStatus === 'Approved'
        );
        this.instructorsList.set(instructors);
      },
      error: () => this.instructorsList.set([]),
    });
  }

  isAdmin = computed(() => this.auth.hasRole('Admin'));

  isPrivileged = computed(() => {
    return this.auth.hasRole('Admin') || this.auth.hasRole('Instructor');
  });

  // Students see ONLY their own records, while Admin sees all records
  myEnrollments = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return [];

    let list = this.enrollmentStore.entities();

    if (!this.isAdmin()) {
      const currentDisplayName = (user.displayName || '').toLowerCase().trim();
      const currentEmailPrefix = (user.email || '').split('@')[0].toLowerCase().trim();

      list = list.filter((e) => {
        const eName = (e.studentName || '').toLowerCase().trim();
        return (
          eName === currentDisplayName ||
          (currentEmailPrefix && eName.includes(currentEmailPrefix))
        );
      });
    }

    const query = this.searchQuery().toLowerCase().trim();
    if (query) {
      list = list.filter(
        (e) =>
          (e.courseName && e.courseName.toLowerCase().includes(query)) ||
          (e.studentName && e.studentName.toLowerCase().includes(query)) ||
          (e.status && e.status.toLowerCase().includes(query)) ||
          (e.notes && e.notes.toLowerCase().includes(query))
      );
    }

    return list;
  });

  // Unique students enrolled across the academy for admin picker
  enrolledStudents = computed<StudentSummary[]>(() => {
    const list = this.enrollmentStore.entities();
    const map = new Map<string, {
      id: number | string;
      name: string;
      email: string;
      coursesCount: number;
      gradedCount: number;
      totalScore: number;
      approvedCount: number;
      gpaPoints: number;
    }>();

    for (const e of list) {
      const sName = (e.studentName || `Student #${e.studentId}`).trim();
      if (!map.has(sName)) {
        map.set(sName, {
          id: e.studentId,
          name: sName,
          email: `${sName.toLowerCase().replace(/\s+/g, '.')}@academy.edu`,
          coursesCount: 0,
          gradedCount: 0,
          totalScore: 0,
          approvedCount: 0,
          gpaPoints: 0,
        });
      }

      const s = map.get(sName)!;
      s.coursesCount++;
      if (e.status === 'Approved') s.approvedCount++;
      if (e.grade !== undefined && e.grade !== null) {
        s.gradedCount++;
        s.totalScore += e.grade;
        const g = e.grade;
        let pts = 4.0;
        if (g < 60) pts = 0.0;
        else if (g < 70) pts = 2.0;
        else if (g < 80) pts = 3.0;
        else if (g < 90) pts = 3.5;
        else pts = 4.0;
        s.gpaPoints += pts;
      }
    }

    const summaries: StudentSummary[] = [];
    map.forEach((s) => {
      const avg = s.gradedCount > 0 ? (s.totalScore / s.gradedCount).toFixed(1) : '85.0';
      const gpa = s.gradedCount > 0 ? (s.gpaPoints / s.gradedCount).toFixed(2) : '3.75';
      summaries.push({
        id: s.id,
        name: s.name,
        email: s.email,
        coursesCount: s.coursesCount,
        gradedCount: s.gradedCount,
        totalScore: s.totalScore,
        approvedCount: s.approvedCount,
        gpa,
        avgScore: avg,
      });
    });

    const q = this.transcriptStudentSearch().toLowerCase().trim();
    if (q) {
      return summaries.filter(
        (s) =>
          s.name.toLowerCase().includes(q) ||
          String(s.id).includes(q) ||
          s.email.toLowerCase().includes(q)
      );
    }

    return summaries;
  });

  // Faculty Workload Calculations for Admin
  facultyWorkload = computed(() => {
    const courses = this.coursesResource.value() || [];
    const instructors = this.instructorsList();

    return instructors.map((inst) => {
      const assigned = courses.filter(
        (c) => String(c.instructorId) === String(inst.id) || (c.instructorName && c.instructorName.includes(inst.displayName || inst.firstName))
      );
      const totalStudents = assigned.reduce((acc, c) => acc + (c.enrollmentCount || 0), 0);
      const maxCap = assigned.reduce((acc, c) => acc + (c.maxCapacity || 1), 0);
      const loadPercentage = maxCap > 0 ? Math.round((totalStudents / maxCap) * 100) : 0;

      return {
        id: inst.id,
        name: inst.displayName || `${inst.firstName} ${inst.lastName}`,
        email: inst.email,
        role: inst.role,
        assignedCoursesCount: assigned.length,
        assignedCourses: assigned,
        totalStudents,
        loadPercentage,
      };
    });
  });

  // Transcript Records strictly scoped for the selected student or current user
  transcriptEnrollments = computed(() => {
    if (!this.isAdmin()) {
      return this.myEnrollments();
    }
    const selected = this.selectedTranscriptStudent();
    if (!selected) return [];
    return this.enrollmentStore.entities().filter(
      (e) => (e.studentName || '').toLowerCase().trim() === selected.toLowerCase().trim()
    );
  });

  transcriptStudentInfo = computed(() => {
    if (!this.isAdmin()) {
      return {
        name: this.studentName(),
        credits: this.earnedCredits(),
        gpa: this.cumulativeGpa(),
        status: this.graduationStatus(),
        email: this.auth.currentUser()?.email || 'student@academy.edu',
      };
    }
    const selected = this.selectedTranscriptStudent();
    const list = this.transcriptEnrollments();
    const graded = list.filter((e) => e.grade !== undefined && e.grade !== null);
    const approved = list.filter((e) => e.status === 'Approved');
    const credits = 45 + approved.length * 3;

    let gpa = '3.80';
    if (graded.length > 0) {
      const points = graded.reduce((acc, curr) => {
        const g = curr.grade ?? 0;
        let pts = 4.0;
        if (g < 60) pts = 0.0;
        else if (g < 70) pts = 2.0;
        else if (g < 80) pts = 3.0;
        else if (g < 90) pts = 3.5;
        else pts = 4.0;
        return acc + pts;
      }, 0);
      gpa = (points / graded.length).toFixed(2);
    }

    return {
      name: selected || 'Selected Student',
      credits,
      gpa,
      status: credits >= 120 ? 'Eligible for Graduation' : 'In Progress',
      email: `${(selected || 'student').toLowerCase().replace(/\s+/g, '.')}@academy.edu`,
    };
  });

  // Academic Grade Distribution & Calculations
  gradeStats = computed(() => {
    const list = this.enrollmentStore.entities();
    const graded = list.filter((e) => e.grade !== undefined && e.grade !== null);

    let aCount = 0;
    let bCount = 0;
    let cCount = 0;
    let dCount = 0;
    let fCount = 0;
    let totalScore = 0;

    for (const item of graded) {
      const g = item.grade ?? 0;
      totalScore += g;
      if (g >= 90) aCount++;
      else if (g >= 80) bCount++;
      else if (g >= 70) cCount++;
      else if (g >= 60) dCount++;
      else fCount++;
    }

    const totalGraded = graded.length || 1;
    const avgScore = graded.length ? (totalScore / graded.length).toFixed(1) : '86.4';
    const certifiedCount = graded.filter((e) => (e.grade ?? 0) >= 70).length;
    const passRate = graded.length ? Math.round((certifiedCount / graded.length) * 100) : 94;

    return {
      totalGraded: graded.length,
      avgScore,
      certifiedCount,
      passRate,
      a: { count: aCount, pct: Math.round((aCount / totalGraded) * 100) },
      b: { count: bCount, pct: Math.round((bCount / totalGraded) * 100) },
      c: { count: cCount, pct: Math.round((cCount / totalGraded) * 100) },
      d: { count: dCount, pct: Math.round((dCount / totalGraded) * 100) },
      f: { count: fCount, pct: Math.round((fCount / totalGraded) * 100) },
    };
  });

  // Registry table records for the registry modal / table view
  registryRecords = computed(() => {
    let list = this.enrollmentStore.entities();
    const q = this.registrySearchQuery().toLowerCase().trim();
    if (q) {
      list = list.filter(
        (e) =>
          (e.studentName && e.studentName.toLowerCase().includes(q)) ||
          (e.courseName && e.courseName.toLowerCase().includes(q)) ||
          (e.notes && e.notes.toLowerCase().includes(q)) ||
          String(e.studentId).includes(q)
      );
    }
    const f = this.registryStatusFilter();
    if (f === 'Graded') {
      list = list.filter((e) => e.grade !== undefined && e.grade !== null);
    } else if (f !== 'all') {
      list = list.filter((e) => e.status === f);
    }
    return list;
  });

  earnedCredits = computed(() => {
    const approved = this.myEnrollments().filter((e) => e.status === 'Approved');
    return 45 + approved.length * 3;
  });

  cumulativeGpa = computed(() => {
    const graded = this.myEnrollments().filter((e) => e.grade !== undefined && e.grade !== null);
    if (!graded.length) return '3.75';

    const totalPoints = graded.reduce((acc, curr) => {
      const g = curr.grade ?? 0;
      let pts = 4.0;
      if (g < 60) pts = 0.0;
      else if (g < 70) pts = 2.0;
      else if (g < 80) pts = 3.0;
      else if (g < 90) pts = 3.5;
      else pts = 4.0;
      return acc + pts;
    }, 0);

    return (totalPoints / graded.length).toFixed(2);
  });

  graduationStatus = computed(() => {
    return this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress';
  });

  selectedCourse = signal<Course | null>(null);

  catalogDepartmentFilter = signal<'all' | 'CS' | 'Cloud' | 'Web' | 'Data'>('all');
  catalogSearchQuery = signal('');

  coursesResource = rxResource<Course[], unknown>({
    stream: () => this.api.getAll(),
  });

  filteredCatalogCourses = computed(() => {
    let list = this.coursesResource.value() || [];
    const dept = this.catalogDepartmentFilter();
    const q = this.catalogSearchQuery().toLowerCase().trim();

    if (dept !== 'all') {
      list = list.filter((c) => {
        const title = (c.title || '').toLowerCase();
        const code = (c.code || '').toLowerCase();
        if (dept === 'CS') return code.startsWith('cs') || title.includes('computer science') || title.includes('algorithm');
        if (dept === 'Cloud') return code.startsWith('dev') || title.includes('cloud') || title.includes('microservices') || title.includes('kubernetes');
        if (dept === 'Web') return code.startsWith('cse') || title.includes('web') || title.includes('api') || title.includes('rest');
        if (dept === 'Data') return code.startsWith('data') || title.includes('data') || title.includes('database') || title.includes('sql');
        return true;
      });
    }

    if (q) {
      list = list.filter((c) =>
        (c.title && c.title.toLowerCase().includes(q)) ||
        (c.code && c.code.toLowerCase().includes(q)) ||
        (c.instructorName && c.instructorName.toLowerCase().includes(q))
      );
    }

    return list;
  });

  isEnrolledInCourse(courseId: number | string): boolean {
    const enrollments = this.myEnrollments();
    return enrollments.some((e) => String(e.courseId) === String(courseId) && e.status === 'Approved');
  }

  isPendingInCourse(courseId: number | string): boolean {
    const enrollments = this.myEnrollments();
    return enrollments.some((e) => String(e.courseId) === String(courseId) && e.status === 'Pending');
  }

  getCurriculumForCourse(course: Course): CourseCurriculum {
    return this.curriculumService.getCurriculum(course.id, course.title, course.code);
  }

  registerForClass() {
    this.router.navigate(['/enroll']);
  }

  handleEnroll(course: Course) {
    this.selectedCourse.set(course);
    this.router.navigate(['/enroll'], { queryParams: { courseId: course.id } });
  }

  openCoursePreview(course: Course) {
    const curr = this.curriculumService.getCurriculum(course.id, course.title, course.code);
    this.previewCourseModal.set({ course, curriculum: curr });
  }

  closeCoursePreview() {
    this.previewCourseModal.set(null);
  }

  openTranscriptModal(studentName?: string) {
    if (this.isAdmin()) {
      if (studentName) {
        this.selectedTranscriptStudent.set(studentName);
        this.transcriptStep.set('view');
      } else {
        this.transcriptStep.set('select');
      }
    } else {
      this.transcriptStep.set('view');
    }
    this.showTranscriptModal.set(true);
  }

  selectStudentForTranscript(student: StudentSummary) {
    this.selectedTranscriptStudent.set(student.name);
    this.transcriptStep.set('view');
  }

  backToStudentSelector() {
    this.transcriptStep.set('select');
  }

  closeTranscriptModal() {
    this.showTranscriptModal.set(false);
    this.transcriptStep.set('select');
  }

  // View Mode / Popups Handlers
  openViewMode(mode: 'cards' | 'distribution' | 'table') {
    this.adminViewMode.set(mode);
    if (mode === 'distribution') {
      this.showAnalyticsModal.set(true);
    } else if (mode === 'table') {
      this.showRegistryModal.set(true);
    }
  }

  closeAnalyticsModal() {
    this.showAnalyticsModal.set(false);
  }

  closeRegistryModal() {
    this.showRegistryModal.set(false);
  }

  openFacultyWorkloadModal() {
    this.showFacultyModal.set(true);
  }

  closeFacultyWorkloadModal() {
    this.showFacultyModal.set(false);
  }

  toggleGpaCalculator() {
    this.showGpaCalculator.update((v) => !v);
  }

  exportRegistryCsv() {
    const list = this.registryRecords();
    const headers = ['ID', 'Student Name', 'Course Name', 'Term', 'Status', 'Grade (%)', 'Letter Grade', 'Notes'];
    const rows = list.map((e) => [
      e.id,
      `"${e.studentName || ''}"`,
      `"${e.courseName || ''}"`,
      'Fall 2026',
      e.status,
      e.grade ?? '',
      e.letterGrade ?? '',
      `"${e.notes || ''}"`,
    ]);

    const csvContent = [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Institutional-Registry-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  printTranscript() {
    window.print();
  }

  viewCertificate(courseName: string, studentRecipient?: string) {
    const recipient = studentRecipient || this.studentName();
    this.activeCertificate.set({
      student: recipient,
      course: courseName,
      code: 'TMS-CERT-' + Math.random().toString(36).substring(2, 8).toUpperCase() + '-2026',
      date: new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }),
    });
  }

  closeCertificate() {
    this.activeCertificate.set(null);
  }

  downloadCertificate() {
    window.print();
  }
}
