import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminApiService } from '../../../core/api/admin-api.service';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface SourceItem {
  id: string;
  title: string;
  status: 'Missing' | 'Blocked' | 'Extractable' | 'Draft' | 'Published';
}

@Component({
  selector: 'app-source-inventory',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './source-inventory.component.html'
})
export class SourceInventoryComponent implements OnInit {
  items: SourceItem[] = [];

  columns: { status: SourceItem['status'], title: string }[] = [
    { status: 'Missing', title: 'Missing' },
    { status: 'Blocked', title: 'Blocked' },
    { status: 'Extractable', title: 'Extractable' },
    { status: 'Draft', title: 'Draft' },
    { status: 'Published', title: 'Published' }
  ];

  constructor(private adminApi: AdminApiService) {}

  ngOnInit() {
    this.adminApi.getSourceInventory().pipe(
      catchError(() => {
        // Dummy data on fail
        const dummyData: SourceItem[] = [
          { id: '1', title: 'Unit 1 Audio', status: 'Missing' },
          { id: '2', title: 'Unit 2 Script', status: 'Missing' },
          { id: '3', title: 'Test A PDF', status: 'Blocked' },
          { id: '4', title: 'Unit 3 Reading', status: 'Extractable' },
          { id: '5', title: 'Unit 4 Video', status: 'Draft' },
          { id: '6', title: 'Unit 1 Grammar', status: 'Published' },
        ];
        return of(dummyData);
      })
    ).subscribe((data: SourceItem[]) => {
      this.items = data;
    });
  }

  getItemsByStatus(status: SourceItem['status']): SourceItem[] {
    return this.items.filter(item => item.status === status);
  }

  getActionText(status: SourceItem['status']): string {
    switch (status) {
      case 'Missing': return 'Add Source';
      case 'Blocked': return 'Resolve';
      case 'Extractable': return 'Extract';
      case 'Draft': return 'Edit';
      case 'Published': return 'View';
      default: return 'Action';
    }
  }
}
