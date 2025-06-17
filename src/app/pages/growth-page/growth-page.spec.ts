import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GrowthPage } from './growth-page';

describe('GrowthPage', () => {
  let component: GrowthPage;
  let fixture: ComponentFixture<GrowthPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GrowthPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GrowthPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
