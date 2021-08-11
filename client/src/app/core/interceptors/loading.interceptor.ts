import {HttpEvent, HttpHandler, HttpInterceptor, HttpRequest} from '@angular/common/http';
import {Injectable} from '@angular/core';
import {BusyService} from '../services/busy.service';
import {Observable} from 'rxjs';
import {delay, finalize} from 'rxjs/operators';



@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  constructor(private busyService: BusyService) {
  }
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    this.busyService.busy();
    return next.handle(req).pipe(
      delay(1000),
      finalize(() => {
        this.busyService.idle();
      })
    );
  }

}
