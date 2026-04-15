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
}

// Aliases for backward compatibility
export type LoginDto = LoginRequestDto;
export type RegisterDto = RegisterRequestDto;
export type AuthResponse = AuthResponseDto;
export type User = UserDto;
