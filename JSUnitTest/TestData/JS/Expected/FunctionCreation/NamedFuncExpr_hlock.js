﻿function test1(){var foo=10;(function foo(n){--n>0&&foo(n)})(10),alert(foo);var n=function local_never_referenced(){};(function local_one_self_ref(n){--n>0&&local_one_self_ref(n)})(10);var ack=function ack(){};ack.bar=10;var trap=function trap(){trap()};return trap.bar=11,n()}var foo=function global_no_references(){};foo=function global_one_self_ref(n){--n>0&&global_one_self_ref(n)};var ack=function ack(){};ack.bar=function(){return ack[12]};var trap=function trap(){trap()};trap.bar=13;function test1(){var a1,n=function a1(){}}function test2(){var n=function a2(){alert(a2)},a2}function test3(){var i=function a3(){alert(a3)},t=function a3(){},n=function a3(){}}function test4(){var i=function a4(){},t=function a4(){},n=function a4(){},a4}function test5(){var a5=function a5(){}}