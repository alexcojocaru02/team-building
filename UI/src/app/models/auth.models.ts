export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface RegisterRequestDto {
  fullName: string;
  email: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
}

export interface UserDto {
  id: string;
  fullName?: string;
  email: string;
  role?: string;
  teamIds?: string[];
  bio?: string;
  avatarUrl?: string;
  department?: string;
  location?: string;
  timezone?: string;
  pronouns?: string;
  preferredWorkStyle?: string;
  hobbies?: string[];
  strengths?: string[];
  icebreaker?: string;
  updatedAt?: string;
}

export interface UpdateProfileDto {
  bio?: string;
  avatarUrl?: string;
  department?: string;
  location?: string;
  timezone?: string;
  pronouns?: string;
  preferredWorkStyle?: string;
  hobbies?: string[];
  strengths?: string[];
  icebreaker?: string;
}

export interface TeamDetailDto {
  id: string;
  name: string;
  ownerId?: string;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
  memberIds: string[];
}

export interface CreateTeamDto {
  name: string;
  description?: string;
}

export interface TeamJoinRequestDto {
  id: string;
  teamId: string;
  teamName: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  status: string;
  createdAt: string;
}

export interface CreateTeamResponseDto {
  team: TeamDetailDto;
  newToken?: string;
}

// Aliases for backward compatibility
export type LoginDto = LoginRequestDto;
export type RegisterDto = RegisterRequestDto;
export type AuthResponse = AuthResponseDto;
export type User = UserDto;
