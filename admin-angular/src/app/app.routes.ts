import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AdminShellComponent } from './layout/admin-shell.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { LoginComponent } from './features/login/login.component';
import { ServicesComponent } from './features/services/services.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'services', component: ServicesComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
