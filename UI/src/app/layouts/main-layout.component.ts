import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SidePanelComponent } from '../components/side-panel/side-panel.component';

@Component({
  standalone: true,
  selector: 'app-main-layout',
  imports: [RouterModule, SidePanelComponent],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent {}
