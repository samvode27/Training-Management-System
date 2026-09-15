import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'tms-analytics-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="analytics-card-inner">
      <!-- Top Overview Stats -->
      <div class="analytics-stat-boxes">
        <!-- Approved -->
        <div class="status-summary-box approved-box">
          <div class="box-header">
            <div class="box-icon approved-icon-bg">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                <polyline points="22 4 12 14.01 9 11.01"></polyline>
              </svg>
            </div>
            <span class="box-status-title">Approved</span>
          </div>
          <div class="box-metrics">
            <span class="box-count">{{ approvedCount() }}</span>
            <span class="box-percent">{{ approvedPercent() }}%</span>
          </div>
          <div class="box-bar-track">
            <div class="box-bar-fill fill-approved" [style.width.%]="approvedPercent()"></div>
          </div>
        </div>

        <!-- Pending -->
        <div class="status-summary-box pending-box">
          <div class="box-header">
            <div class="box-icon pending-icon-bg">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="12 6 12 12 16 14"></polyline>
              </svg>
            </div>
            <span class="box-status-title">Pending Action</span>
          </div>
          <div class="box-metrics">
            <span class="box-count">{{ pendingCount() }}</span>
            <span class="box-percent">{{ pendingPercent() }}%</span>
          </div>
          <div class="box-bar-track">
            <div class="box-bar-fill fill-pending" [style.width.%]="pendingPercent()"></div>
          </div>
        </div>

        <!-- Rejected -->
        <div class="status-summary-box rejected-box">
          <div class="box-header">
            <div class="box-icon rejected-icon-bg">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="15" y1="9" x2="9" y2="15"></line>
                <line x1="9" y1="9" x2="15" y2="15"></line>
              </svg>
            </div>
            <span class="box-status-title">Declined</span>
          </div>
          <div class="box-metrics">
            <span class="box-count">{{ rejectedCount() }}</span>
            <span class="box-percent">{{ rejectedPercent() }}%</span>
          </div>
          <div class="box-bar-track">
            <div class="box-bar-fill fill-rejected" [style.width.%]="rejectedPercent()"></div>
          </div>
        </div>
      </div>

      <!-- Segmented Distribution Bar -->
      <div class="distribution-section">
        <div class="distribution-header">
          <span class="dist-label">Total Cohort Allocation Pipeline</span>
          <span class="dist-total">{{ data().length }} Enrolled Records</span>
        </div>
        <div class="multi-segment-track">
          <div class="segment seg-approved" [style.width.%]="approvedPercent()" title="Approved: {{ approvedCount() }}"></div>
          <div class="segment seg-pending" [style.width.%]="pendingPercent()" title="Pending: {{ pendingCount() }}"></div>
          <div class="segment seg-rejected" [style.width.%]="rejectedPercent()" title="Rejected: {{ rejectedCount() }}"></div>
        </div>
        <div class="distribution-legend">
          <div class="legend-item"><span class="legend-dot dot-approved"></span> Approved ({{ approvedCount() }})</div>
          <div class="legend-item"><span class="legend-dot dot-pending"></span> Pending ({{ pendingCount() }})</div>
          <div class="legend-item"><span class="legend-dot dot-rejected"></span> Declined ({{ rejectedCount() }})</div>
        </div>
      </div>
    </div>
  `,
  styleUrl: './analytics-chart.component.scss',
})
export class AnalyticsChartComponent {
  data = input.required<Enrollment[]>();

  approvedCount = computed(() => this.data().filter((e) => e.status === 'Approved').length);
  pendingCount = computed(() => this.data().filter((e) => e.status === 'Pending').length);
  rejectedCount = computed(() => this.data().filter((e) => e.status === 'Rejected').length);

  totalCount = computed(() => Math.max(this.data().length, 1));

  approvedPercent = computed(() => Math.round((this.approvedCount() / this.totalCount()) * 100));
  pendingPercent = computed(() => Math.round((this.pendingCount() / this.totalCount()) * 100));
  rejectedPercent = computed(() => Math.round((this.rejectedCount() / this.totalCount()) * 100));
}