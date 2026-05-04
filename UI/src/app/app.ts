import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';
import { SidePanelComponent } from './components/side-panel/side-panel.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterModule,
    MatButtonModule,
    SidePanelComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected title = 'team-building';
}
