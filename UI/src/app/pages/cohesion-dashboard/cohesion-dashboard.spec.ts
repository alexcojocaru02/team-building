import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CohesionDashboard } from './cohesion-dashboard';

describe('CohesionDashboard', () => {
  let component: CohesionDashboard;
  let fixture: ComponentFixture<CohesionDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CohesionDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CohesionDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
