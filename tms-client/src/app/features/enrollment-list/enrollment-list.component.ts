import { Component, viewChild, effect, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth.service';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'tms-enrollment-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
  ],
  templateUrl: './enrollment-list.component.html',
  styleUrl: './enrollment-list.component.scss',
})
export class EnrollmentListComponent implements OnInit {
  store = inject(EnrollmentStore);
  auth = inject(AuthService);
  displayedColumns = ['studentName', 'courseName', 'academicTerm', 'status', 'grade', 'actions'];

  searchTerm = signal('');
  statusFilter = signal<'ALL' | 'Pending' | 'Approved' | 'Rejected'>('ALL');

  dataSource = new MatTableDataSource<Enrollment>();

  readonly paginator = viewChild(MatPaginator);
  readonly sort = viewChild(MatSort);

  constructor() {
    this.dataSource.filterPredicate = (data: Enrollment, filterJson: string): boolean => {
      try {
        const criteria = JSON.parse(filterJson);
        const term = (criteria.term || '').toLowerCase().trim();
        const status = criteria.status || 'ALL';

        const matchesStatus = status === 'ALL' || data.status === status;
        const matchesTerm =
          !term ||
          Boolean(data.studentName && data.studentName.toLowerCase().includes(term)) ||
          Boolean(data.courseName && data.courseName.toLowerCase().includes(term)) ||
          Boolean(data.notes && data.notes.toLowerCase().includes(term)) ||
          Boolean(data.studentId && data.studentId.toString().includes(term)) ||
          Boolean(data.id && data.id.toString().toLowerCase().includes(term));

        return Boolean(matchesStatus && matchesTerm);
      } catch {
        return true;
      }
    };

    effect(() => {
      this.dataSource.data = this.store.scopedEnrollments();
      this.applyFilter();
    });

    effect(() => {
      const p = this.paginator();
      if (p) this.dataSource.paginator = p;
      const s = this.sort();
      if (s) this.dataSource.sort = s;
    });
  }

  ngOnInit() {
    // Strictly lock scope based on role: Admin = 'all', Instructor = 'my'
    if (this.auth.hasRole('Admin')) {
      this.store.setScope('all');
    } else {
      this.store.setScope('my');
    }

    this.store.loadEnrollments();
    this.store.listenForLiveUpdates();
  }

  isOwner(row: Enrollment): boolean {
    return this.auth.isCourseOwner(row.courseInstructorId);
  }

  onSearchChange(term: string) {
    this.searchTerm.set(term);
    this.applyFilter();
  }

  setStatusFilter(status: 'ALL' | 'Pending' | 'Approved' | 'Rejected') {
    this.statusFilter.set(status);
    this.applyFilter();
  }

  private applyFilter() {
    this.dataSource.filter = JSON.stringify({
      term: this.searchTerm(),
      status: this.statusFilter(),
    });
    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  exportCsv() {
    const data = this.dataSource.filteredData || this.dataSource.data;
    if (!data.length) return;

    const headers = [
      'Enrollment ID',
      'Student Name',
      'Student ID',
      'Course Name',
      'Academic Term',
      'Student Goals / Notes',
      'Status',
      'Grade Score',
      'Letter Grade',
      'Enrolled Date',
    ];
    const rows = data.map((e) => [
      `"${e.id}"`,
      `"${e.studentName || ''}"`,
      `"${e.studentId || ''}"`,
      `"${e.courseName || ''}"`,
      `"Institutional Term 2026"`,
      `"${e.notes ? e.notes.replace(/"/g, '""') : 'Standard Academic Track'}"`,
      `"${e.status || ''}"`,
      `"${e.grade !== undefined && e.grade !== null ? e.grade : 'N/A'}"`,
      `"${e.letterGrade || 'N/A'}"`,
      `"${e.enrolledAt ? new Date(e.enrolledAt).toISOString() : ''}"`,
    ]);

    const csvContent =
      'data:text/csv;charset=utf-8,' +
      [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute(
      'download',
      `TMS_${this.auth.hasRole('Admin') ? 'Academy' : 'Cohort'}_Enrollments_${new Date()
        .toISOString()
        .slice(0, 10)}.csv`
    );
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}