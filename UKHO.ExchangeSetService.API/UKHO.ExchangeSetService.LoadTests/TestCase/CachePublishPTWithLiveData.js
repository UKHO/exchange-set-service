import http from 'k6/http';
import { check } from 'k6';
import { scenario } from 'k6/execution';
import exec from 'k6/execution';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { Trend } from 'k6/metrics';

const publishEvent = new Trend('NewFilePublishEvent');
const TestData = require('../scripts/CacheTestData.js');
const config=JSON.parse(open('./config.json'));
const publishEventLogsFile = JSON.parse(open('../TestData/encPublishRecords.json'));

export let options = {

  scenarios: {
    'NewPublish_400PerHour':{
      executor: 'constant-arrival-rate',
      duration:'1h',
      rate:100,
      timeUnit:'15m',
      preAllocatedVUS:1
     },

    'NewPublish_800PerHour':{
      executor: 'constant-arrival-rate',
      duration:'1h',
      rate:200,
      timeUnit:'15m',
      preAllocatedVUS:1
    },

    'NewPublish_1600PerHour':{
      executor: 'constant-arrival-rate',
      duration:'1h',
      rate:400,
      timeUnit:'15m',
      preAllocatedVUS:2
    },

    'NewPublish_200In30Min_Ramping': {
        executor: 'ramping-arrival-rate',
        startRate: 0,
        timeUnit: '1s',
        preAllocatedVUS: 10,
        stages: [
            { target: 6, duration: '5s' },
            { target: 7, duration: '5s' },
            { target: 3, duration: '5s' },
            { target: 10, duration: '5s' },
            { target: 12, duration: '5s' },
            { target: 2, duration: '5s' },
        ]
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
    let reqData = TestData.getNextRecord(publishEventLogsFile, scenario.iterationInTest % publishEventLogsFile.length);
    ReplayRequest(reqData);
}


export function ReplayRequest(reqData){
    let essRes =  http.post(reqData.url,reqData.requestBodyText, 
    { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    publishEvent.add(essRes.timings.duration);

    check(essRes,{
        'HTTP Response Code':(essRes)=>essRes.status===200
    });
    
    if(essRes.status==200){
        let resHeader = essRes.headers['X-Correlation-Id'];
        console.log('Correlation Id-->' + resHeader);
    }

    else if(essRes.status==401){ //Unauthorized
        console.error("Bearer token expired!");
        exec.test.abort("Please update token, Execution Stopped!");
    }
    
    else{
        console.error("Response Code:"+essRes.status,"URL:"+essRes.request.url,"Payload:"+essRes.request.body);
    }

}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/CachePublish-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/CachePublish-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}

