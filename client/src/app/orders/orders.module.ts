import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrdersComponent } from './orders.component';
import { OrderDetailedComponent } from './order-detailed/order-detailed.component';
import {RouterModule} from "@angular/router";
import {SharedModule} from "../shared/shared.module";
import {OrdersRoutingModule} from "./orders-routing.module";



@NgModule({
  declarations: [
    OrdersComponent,
    OrderDetailedComponent
  ],
    imports: [
        CommonModule,
        RouterModule,
        SharedModule,
        OrdersRoutingModule
    ]
})
export class OrdersModule { }
