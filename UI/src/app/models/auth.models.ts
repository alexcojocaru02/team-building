export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  name?: string;
}

export interface AuthResponse {
  token: string;
}

export interface User {
  id: string;
  email: string;
  name?: string;
}
