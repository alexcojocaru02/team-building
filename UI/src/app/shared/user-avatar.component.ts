import { Component, computed, input } from '@angular/core';

const AVATAR_PALETTE = [
  '#0ea5e9', '#2563eb', '#4f46e5', '#7c3aed', '#9333ea',
  '#c026d3', '#db2777', '#e11d48', '#dc2626', '#ea580c',
  '#d97706', '#65a30d', '#16a34a', '#0d9488',
];

export function getInitials(name?: string | null): string {
  const trimmed = (name || '').trim();
  if (!trimmed) return '?';
  const parts = trimmed.split(/\s+/).filter(Boolean);
  return parts.length >= 2
    ? (parts[0][0] + parts[1][0]).toUpperCase()
    : trimmed.slice(0, 2).toUpperCase();
}

export function getAvatarColor(seed?: string | null): string {
  const key = (seed || '').trim().toLowerCase();
  if (!key) return AVATAR_PALETTE[0];
  let hash = 0;
  for (let i = 0; i < key.length; i++) {
    hash = key.charCodeAt(i) + ((hash << 5) - hash);
  }
  return AVATAR_PALETTE[Math.abs(hash) % AVATAR_PALETTE.length];
}

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  template: `
    <div
      class="user-avatar"
      [style.background-color]="color()"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [style.font-size.px]="fontSize()"
    >{{ initials() }}</div>
  `,
  styles: [`
    .user-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      border-radius: 9999px;
      color: white;
      font-weight: 600;
      line-height: 1;
    }
  `],
})
export class UserAvatarComponent {
  name = input<string | null | undefined>('');
  size = input<number>(36);

  fontSize = computed(() => Math.round(this.size() * 0.4));
  initials = computed(() => getInitials(this.name()));
  color = computed(() => getAvatarColor(this.name()));
}
