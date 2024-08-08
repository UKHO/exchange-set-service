import http from 'k6/http';
import { check,sleep,metrics,output } from 'k6';
import { vu,scenario,instance } from 'k6/execution';
import exec from 'k6/execution';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { Trend } from 'k6/metrics';

const publishEvent = new Trend('NewFilePublishEvent');
const TestData = require('../scripts/CacheTestData.js')
const config=JSON.parse(open('./config.json'));
const publishLogFile = JSON.parse(open('../LiveLogs/FilePublishedLogs.json'));
const ESSPublishFile = JSON.parse(open('../LiveLogs/ESSPublishedLogs.json'));
const logParser=require('../helper/LogParser.js');
const ess=require('../scripts/ReplayRequest.js');
let retryCount=0;

export let options = {
  scenarios: {

    'NewPublish_400PerHour':{
      executor: 'constant-arrival-rate',
      duration:'1m',
      rate:100,
      timeUnit:'15s',
      preAllocatedVUS:2,
    },

    'NewPublish_200-Min30_Ramping':{ 
      executor: 'ramping-arrival-rate',
      startRate:0,
      timeUnit: '1s',
      preAllocatedVUS:10,
      stages: [
        { target: 6, duration: '5s' }, // 5RPM=30

        { target: 7, duration: '5s' }, //7 RPM=35+15
        { target: 3, duration: '5s' },
        
        { target: 10, duration: '5s' }, //10 RPM=50+60
        { target: 12, duration: '5s' }, 
        
        { target: 2, duration: '5s' }, //2 RPM=10
      ]
    },

    'NewPublish_800PerHour':{
      executor: 'constant-arrival-rate',
      duration:'15m',
      rate:200,
      timeUnit:'15m',
      preAllocatedVUS:1,
      startTime:'15m',
    },

    'NewPublish_1600PerHour':{
      executor: 'constant-arrival-rate',
      duration:'1h',
      rate:1600,
      timeUnit:'1h',
      preAllocatedVUS:2,
    },
  },
};

export function setup() {
  const testStartTime = new Date();
  console.warn("Test Start Time:" + testStartTime.toLocaleTimeString('en-US'));
  console.warn("Test Start Time(UTC):" + testStartTime.toUTCString());
}

export function teardown() {
  const testEndTime = new Date();
  console.warn("Test End Time:" + testEndTime.toLocaleTimeString('en-US'));
  console.warn("Test End Time(UTC):" + testEndTime.toUTCString());
}

export default function publishNewFile() {
  let reqData=TestData.getNextRecord(publishLogFile,scenario.iterationInTest) //% ESSPublishFile.length)
    console.log("Req Data:"+reqData.requestBodyText)
      // ess.ReplayRequest(reqData);
      ReplayRequest(reqData);
}


export function ReplayRequest(reqData){
    let essRes =  http.post(reqData.url,reqData.requestBodyText, 
    { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    // if(essRes.status==500){
    //   console.warn(`Failed Retrying after 90 seconds`)
    //   sleep(90)
    //   essRes =  http.post(reqData.url,reqData.requestBodyText, 
    //     { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });
    // }

    publishEvent.add(essRes.timings.duration)

    check(essRes,{
        'HTTP Response Code':(essRes)=>essRes.status===200,
    });
    
    if(essRes.status==200){
      let resHeader=essRes.headers['X-Correlation-Id']
      console.log('Correlation Id-->'+resHeader);
    }

    else if(essRes.status==401){ //Unauthorized
        console.error("Bearer token expired!")
        exec.test.abort("Please update token, Execution Stopped!")
    }
    
    else{
        console.error("Response Code:"+essRes.status,"URL:"+essRes.request.url,"Payload:"+essRes.request.body);
    }

}

export function handleFlakyRequest(reqData){

}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/CachePublish-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/CachePublish-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}

