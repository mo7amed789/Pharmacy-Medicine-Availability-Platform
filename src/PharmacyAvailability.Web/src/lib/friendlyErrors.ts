/**
 * Converts raw API errors and technical exceptions into clear, empathetic,
 * user-friendly messages for customers and pharmacy staff.
 */
export function getFriendlyErrorMessage(rawError: unknown, language: 'ar' | 'en' = 'ar'): string {
  if (!rawError) {
    return language === 'ar' ? 'حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.' : 'An unexpected error occurred. Please try again.';
  }

  const rawMessage = typeof rawError === 'string'
    ? rawError
    : (rawError as any)?.detail || (rawError as any)?.title || (rawError as any)?.message || '';

  const lower = rawMessage.toLowerCase();

  // Active Hold Conflict
  if ((lower.includes('already exists') || lower.includes('already placed') || lower.includes('active hold') || lower.includes('existing hold')) && (lower.includes('hold') || lower.includes('reservation') || lower.includes('item') || lower.includes('user'))) {
    return language === 'ar'
      ? 'لديك حجز جارٍ بالفعل لهذا الدواء لمدة 60 دقيقة في الصيدلية. يمكنك مراجعة كود الحجز وإدارته من صفحة (حجوزاتي).'
      : 'You already have an active 60-minute hold for this medicine. You can check your pickup code in the "My Holds" tab.';
  }

  // Stock Depletion
  if (lower.includes('insufficient') || lower.includes('stock') || lower.includes('out of stock')) {
    return language === 'ar'
      ? 'نعتذر بشدة، لقد نفدت الكمية المتاحة في هذه الصيدلية للتو قبل إتمام الحجز. يرجى اختيار صيدلية بديلة من القائمة القريبة.'
      : 'We apologize, the available stock at this pharmacy was just reserved by another customer. Please choose an alternative nearby branch.';
  }

  // Branch Closed
  if (lower.includes('closed') || lower.includes('branch is inactive')) {
    return language === 'ar'
      ? 'هذا الفرع مغلق حالياً ولا يستقبل طلبات الحجز. يرجى اختيار فرع آخر مفتوح لاستلام الدواء.'
      : 'This branch is currently closed and not taking reservations. Please pick an open branch.';
  }

  // Invalid Credentials / 401 Unauthorized
  if (
    (rawError as any)?.status === 401 ||
    (rawError as any)?.isUnauthorized ||
    lower.includes('invalid credentials') ||
    lower.includes('invalid email') ||
    lower.includes('password') ||
    lower.includes('unauthorized') ||
    lower.includes('account not found') ||
    lower.includes('not found or inactive') ||
    lower.includes('401')
  ) {
    return language === 'ar'
      ? 'البريد الإلكتروني أو كلمة المرور غير صحيحة. يرجى التحقق من البيانات والمحاولة مرة أخرى.'
      : 'The email address or password is incorrect. Please verify your credentials and try again.';
  }

  // Forbidden / 403 Access Denied
  if (
    (rawError as any)?.status === 403 ||
    (rawError as any)?.isForbidden ||
    lower.includes('forbidden') ||
    lower.includes('access denied') ||
    lower.includes('permission') ||
    lower.includes('403')
  ) {
    return language === 'ar'
      ? 'ليس لديك الصلاحيات الكافية للوصول إلى هذا المحتوى أو المورد.'
      : 'You do not have sufficient permissions to access this content.';
  }

  // Email Already Registered
  if (lower.includes('email') && lower.includes('exists')) {
    return language === 'ar'
      ? 'هذا البريد الإلكتروني مسجل لدينا بالفعل. يمكنك تسجيل الدخول مباشرة إلى حسابك.'
      : 'This email address is already registered. Please sign in instead.';
  }

  // Phone Already Registered
  if (lower.includes('phone') && lower.includes('exists')) {
    return language === 'ar'
      ? 'رقم الهاتف المحمول هذا مسجل بالفعل لحساب آخر. يرجى استخدام رقمك الشخصي الصحيح.'
      : 'This phone number is already registered to another account. Please use your personal number.';
  }

  // Verification OTP Code
  if (lower.includes('code') && (lower.includes('invalid') || lower.includes('expired'))) {
    return language === 'ar'
      ? 'رمز التحقق غير صحيح أو انتهت صلاحيته. يرجى طلب رمز جديد عبر زر (إعادة إرسال الرمز).'
      : 'The verification code is invalid or has expired. Please request a new code.';
  }

  // Network or Connection failure
  if (lower.includes('network') || lower.includes('failed to fetch') || lower.includes('econnrefused')) {
    return language === 'ar'
      ? 'تعذر الاتصال بالخادم. يرجى التحقق من اتصال الإنترنت والضغط على زر (إعادة المحاولة).'
      : 'Unable to reach the server. Please check your internet connection and try again.';
  }

  // Fallback to raw message if it's already in Arabic or reasonable
  if (rawMessage && !rawMessage.startsWith('http') && rawMessage.length < 200) {
    return rawMessage;
  }

  return language === 'ar'
    ? 'تعذر إتمام العملية حالياً. يرجى المحاولة مرة أخرى بعد لحظات.'
    : 'Unable to complete the operation. Please try again shortly.';
}
