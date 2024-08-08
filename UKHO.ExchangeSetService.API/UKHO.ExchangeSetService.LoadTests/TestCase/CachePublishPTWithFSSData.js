import http from 'k6/http';
import { check, sleep, metrics, output } from 'k6';
import { vu, scenario, instance } from 'k6/execution';
import exec from 'k6/execution';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { Trend } from 'k6/metrics';

const publishEvent = new Trend('NewFilePublishEvent');
const TestData = require('../scripts/CacheTestData.js')
const config = JSON.parse(open('./config.json'));

export let options = {
  scenarios: {
    'NewPublish_400PerHour':{
      executor: 'constant-arrival-rate',
      duration:'15m',
      rate:400,
      timeUnit:'15m',
      preAllocatedVUS:1,
    },

    'NewPublish_800PerHour':{
      executor: 'constant-arrival-rate',
      duration:'15m',
      rate:800,
      timeUnit:'15m',
      preAllocatedVUS:1,
      startTime:'15m',
    },

    'NewPublish_1600PerHour': {
      executor: 'constant-arrival-rate',
      duration: '15m',
      rate: 1600,
      timeUnit: '15m',
      preAllocatedVUS: 5,
      startTime:'30m',
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
  // let reqData=TestData.getSingleProductPayloadData(scenario.iterationInTest);
  let reqData = TestData.getNextENCUpdateRecord();
  ReplayRequest(reqData);
}

export function ReplayRequest(reqData) {
  let essRes = http.post(config.Base_URL + config.FilesPublished, reqData,
    { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

  publishEvent.add(essRes.timings.duration)

  check(essRes, {
    'HTTP Response Code': (essRes) => essRes.status === 200,
  });

  if (essRes.status == 200) {
    let resHeader = essRes.headers['X-Correlation-Id']
    // console.info("URL:"+essRes.request.url)
    // console.info("Payload:"+essRes.request.body)
    console.info('Correlation Id-->' + resHeader);
  }

  else if (essRes.status == 401) { //Unauthorized
    console.error("Bearer token expired!")
    exec.test.abort("Please update token, Execution Stopped!")
  }

  else {
    console.error("Response Code:" + essRes.status);
    console.error("URL:" + essRes.request.url)
    console.error("Payload:" + essRes.request.body)
  }

}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/CachePublish-SanityTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/CachePublish-SanityTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}

