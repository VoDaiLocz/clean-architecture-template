import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, Home, BookOpen, BarChart2 } from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  readonly HomeIcon = Home;
  readonly BookIcon = BookOpen;
  readonly ChartIcon = BarChart2;
}
