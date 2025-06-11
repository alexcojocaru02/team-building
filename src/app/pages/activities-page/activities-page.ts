import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-activities-page',
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    RouterModule
  ],
  templateUrl: './activities-page.html',
  styleUrl: './activities-page.scss',
})
export class ActivitiesPage {
  games = [
    {
      name: 'Draw Battle',
      url: 'https://drawbattle.io/',
      description: 'Collaborative pictionary-style game. Fun and creative!',
    },
    {
      name: 'JigsawPuzzles.io',
      url: 'https://jigsawpuzzles.io/',
      description: 'Solve puzzles together with your teammates in real time.',
    },
    {
      name: 'Gartic Phone',
      url: 'https://garticphone.com/',
      description:
        'A hilarious drawing + telephone game to play with the team.',
    },
    {
      name: 'Skribbl.io',
      url: 'https://skribbl.io/',
      description: 'Classic drawing and guessing game — multiplayer friendly.',
    },
    {
      name: 'Codenames',
      url: 'https://codenames.game/room/create',
      description: 'Word association game where teams compete to guess words based on clues.',
    },
  ];

  openGame(url: string) {
    window.open(url, '_blank', 'noopener');
  }
}
