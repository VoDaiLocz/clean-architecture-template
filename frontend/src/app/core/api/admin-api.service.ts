import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private baseUrl = '/api/admin';

  constructor(private http: HttpClient) {}

  getSourceInventory(): Observable<any> {
    return this.http.get(`${this.baseUrl}/inventory`);
  }
}
