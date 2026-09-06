import { z } from 'zod';

export const registerPharmacySchema = z.object({
  name: z.string().trim().min(2, 'Pharmacy name must be at least 2 characters').max(150),
  licenseNumber: z.string().trim().min(3, 'License number must be at least 3 characters').max(50),
  address: z.string().trim().min(5, 'Address must be at least 5 characters').max(250),
  city: z.string().trim().min(2, 'City is required').max(100),
  latitude: z.number().min(-90, 'Latitude must be >= -90').max(90, 'Latitude must be <= 90'),
  longitude: z.number().min(-180, 'Longitude must be >= -180').max(180, 'Longitude must be <= 180'),
  phone: z.string().trim().regex(/^\+?[0-9]{8,15}$/, 'Invalid phone number format'),
  email: z.string().trim().email('Invalid email address').optional().or(z.literal('')),
  openingHours: z.string().max(100).optional().or(z.literal('')),
  adminFullName: z.string().trim().min(2, 'Administrator full name is required').max(150),
  adminEmail: z.string().trim().email('Valid administrator email is required'),
  adminPassword: z.string().min(8, 'Administrator password must be at least 8 characters'),
  adminPhone: z.string().trim().regex(/^\+?[0-9]{8,15}$/, 'Invalid admin phone format').optional().or(z.literal('')),
});

export type RegisterPharmacyFormData = z.infer<typeof registerPharmacySchema>;

export const branchSchema = z.object({
  name: z.string().trim().min(2, 'Branch name is required').max(150),
  address: z.string().trim().min(5, 'Address is required').max(250),
  city: z.string().trim().min(2, 'City is required').max(100),
  latitude: z.number().min(-90).max(90),
  longitude: z.number().min(-180).max(180),
  phone: z.string().trim().regex(/^\+?[0-9]{8,15}$/, 'Invalid phone number format'),
  openingHours: z.string().max(100).optional().or(z.literal('')),
  branchCode: z.string().max(50).optional().or(z.literal('')),
  isActive: z.boolean().default(true),
});

export type BranchFormData = z.infer<typeof branchSchema>;

export const pharmacyProfileSchema = z.object({
  name: z.string().trim().min(2, 'Pharmacy name is required').max(150),
  address: z.string().trim().min(5, 'Address is required').max(250),
  city: z.string().trim().min(2, 'City is required').max(100),
  phone: z.string().trim().regex(/^\+?[0-9]{8,15}$/, 'Invalid phone number format'),
  email: z.string().trim().email('Invalid email address').optional().or(z.literal('')),
  openingHours: z.string().max(100).optional().or(z.literal('')),
});

export type PharmacyProfileFormData = z.infer<typeof pharmacyProfileSchema>;

export const inviteStaffSchema = z.object({
  email: z.string().trim().email('Valid staff email is required'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  fullName: z.string().trim().min(2, 'Full name is required').max(150),
  phoneNumber: z.string().trim().regex(/^\+?[0-9]{8,15}$/, 'Invalid phone number format'),
  role: z.enum(['PharmacyStaff', 'PharmacyAdmin']),
});

export type InviteStaffFormData = z.infer<typeof inviteStaffSchema>;
