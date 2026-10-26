import axios, { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import { ChatMessage, Intent, ActionResult, AIResponse } from '@/types/chat';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';

class ApiClient {
  private client: AxiosInstance;
  private token: string | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: `${API_URL}/api/v1`,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000,
    });

    // Request interceptor for auth
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        if (this.token) {
          config.headers.Authorization = `Bearer ${this.token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.clearToken();
          // Redirect to login or emit event
          if (typeof window !== 'undefined') {
            window.dispatchEvent(new CustomEvent('auth:unauthorized'));
          }
        }
        return Promise.reject(error);
      }
    );
  }

  setToken(token: string | null) {
    this.token = token;
    if (typeof window !== 'undefined') {
      if (token) {
        localStorage.setItem('auth_token', token);
      } else {
        localStorage.removeItem('auth_token');
      }
    }
  }

  getToken(): string | null {
    if (!this.token && typeof window !== 'undefined') {
      this.token = localStorage.getItem('auth_token');
    }
    return this.token;
  }

  clearToken() {
    this.token = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token');
    }
  }

  // Auth
  async login(email: string, password: string): Promise<{ token: string; user: any }> {
    const response = await this.client.post('/auth/login', { email, password });
    const { token, ...user } = response.data;
    this.setToken(token);
    return { token, user };
  }

  async logout(): Promise<void> {
    this.clearToken();
  }

  // Departments
  async getDepartments(): Promise<any[]> {
    const response = await this.client.get('/departments');
    return response.data;
  }

  async createDepartment(name: string): Promise<any> {
    const response = await this.client.post('/departments', { name });
    return response.data;
  }

  async updateDepartment(id: string, name: string): Promise<any> {
    const response = await this.client.put(`/departments/${id}`, { name });
    return response.data;
  }

  async deactivateDepartment(id: string): Promise<void> {
    await this.client.delete(`/departments/${id}`);
  }

  // Users
  async getUsers(): Promise<any[]> {
    const response = await this.client.get('/users');
    return response.data;
  }

  async getUsersByDepartment(departmentId: string): Promise<any[]> {
    const response = await this.client.get(`/users/department/${departmentId}`);
    return response.data;
  }

  async createUser(data: any): Promise<any> {
    const response = await this.client.post('/users', data);
    return response.data;
  }

  async updateUser(id: string, data: any): Promise<any> {
    const response = await this.client.put(`/users/${id}`, data);
    return response.data;
  }

  async deactivateUser(id: string): Promise<void> {
    await this.client.delete(`/users/${id}`);
  }

  // Projects
  async getProjects(params?: { departmentId?: string; status?: string; ownerUserId?: string }): Promise<any[]> {
    const response = await this.client.get('/projects', { params });
    return response.data;
  }

  async getProject(id: string): Promise<any> {
    const response = await this.client.get(`/projects/${id}`);
    return response.data;
  }

  async createProject(data: any): Promise<any> {
    const response = await this.client.post('/projects', data);
    return response.data;
  }

  async updateProject(id: string, data: any, version: number): Promise<any> {
    const response = await this.client.put(`/projects/${id}`, data, {
      headers: { 'If-Match': version.toString() },
    });
    return response.data;
  }

  async deleteProject(id: string, version: number): Promise<void> {
    await this.client.delete(`/projects/${id}`, {
      headers: { 'If-Match': version.toString() },
    });
  }

  async updateProjectStatus(id: string, status: string, version: number): Promise<void> {
    await this.client.patch(`/projects/${id}/status`, { status }, {
      headers: { 'If-Match': version.toString() },
    });
  }

  // Tasks
  async getProjectTasks(projectId: string): Promise<any[]> {
    const response = await this.client.get(`/projects/${projectId}/tasks`);
    return response.data;
  }

  async getTask(id: string): Promise<any> {
    const response = await this.client.get(`/tasks/${id}`);
    return response.data;
  }

  async createTask(projectId: string, data: any): Promise<any> {
    const response = await this.client.post(`/projects/${projectId}/tasks`, data);
    return response.data;
  }

  async updateTask(id: string, data: any, version: number): Promise<any> {
    const response = await this.client.put(`/tasks/${id}`, data, {
      headers: { 'If-Match': version.toString() },
    });
    return response.data;
  }

  async updateTaskStatus(id: string, status: string, version: number): Promise<void> {
    await this.client.patch(`/tasks/${id}/status`, { status }, {
      headers: { 'If-Match': version.toString() },
    });
  }

  async deleteTask(id: string, version: number): Promise<void> {
    await this.client.delete(`/tasks/${id}`, {
      headers: { 'If-Match': version.toString() },
    });
  }

  // Reports
  async getDepartmentProgress(departmentId?: string): Promise<any[]> {
    const response = await this.client.get('/reports/departments/progress', {
      params: { departmentId },
    });
    return response.data;
  }

  async getProjectSummary(projectId: string): Promise<any> {
    const response = await this.client.get(`/reports/projects/${projectId}/summary`);
    return response.data;
  }

  async exportReport(format: 'json' | 'csv', departmentId?: string): Promise<Blob> {
    const response = await this.client.get('/reports/departments/progress/export', {
      params: { format, departmentId },
      responseType: 'blob',
    });
    return response.data;
  }

  // Health
  async checkHealth(): Promise<{ status: string }> {
    const response = await this.client.get('/health/ready');
    return response.data;
  }

  // AI Chat (if backend endpoint exists)
  async sendChatMessage(message: string, context: any): Promise<AIResponse> {
    const response = await this.client.post('/ai/chat', { message, context });
    return response.data;
  }

  async streamChatMessage(message: string, context: any): Promise<ReadableStream> {
    const response = await this.client.post('/ai/chat/stream', { message, context }, {
      responseType: 'stream',
    });
    return response.data;
  }
}

export const apiClient = new ApiClient();

// Initialize token from localStorage on client side
if (typeof window !== 'undefined') {
  const storedToken = localStorage.getItem('auth_token');
  if (storedToken) {
    apiClient.setToken(storedToken);
  }
}