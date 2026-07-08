import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AdminShellComponent } from './layout/admin-shell.component';
import { AppointmentsComponent } from './features/appointments';
import { DashboardComponent } from './features/dashboard';
import { LoginComponent } from './features/login';
import { ServicesComponent } from './features/services';
import { StaffComponent } from './features/staff';
import { StaffDetailComponent } from './features/staff-detail';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'appointments', component: AppointmentsComponent },
      { path: 'services', component: ServicesComponent },
      { path: 'staff', component: StaffComponent },
      { path: 'staff/:id', component: StaffDetailComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
