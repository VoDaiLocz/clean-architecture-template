import { HttpInterceptorFn } from '@angular/common/http';

export const apiInterceptor: HttpInterceptorFn = (request, next) => {
  const requestWithHeaders = request.clone({
    setHeaders: {
      'X-TOEIC-Client': 'angular-ocean-classroom',
    },
  });

  return next(requestWithHeaders);
};
