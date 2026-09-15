import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth.service';
import { AnalyticsChartComponent } from '../../ui/analytics-chart/analytics-chart.component';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, AnalyticsChartComponent],
  templateUrl: './instructor-dashboard.component.html',
  styleUrl: './instructor-dashboard.component.scss',
})
export class InstructorDashboardComponent implements OnInit {
  store = inject(EnrollmentStore);
  auth = inject(AuthService);

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
}