import { Component, input, output } from '@angular/core';
import { Course } from '../../models/course.model';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'tms-course-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './course-card.component.html',
  styleUrl: './course-card.component.scss',
})
export class CourseCardComponent {
  // input.required<Course>() - This declares that the parent must pass a Course
  // Think of it like a required parameter on a C# method
  course = input.required<Course>();

  // output<Course>() - This declares this component can emit a Course event
  // The parent listens for this event like a button click
  enrollClicked = output<Course>();
}