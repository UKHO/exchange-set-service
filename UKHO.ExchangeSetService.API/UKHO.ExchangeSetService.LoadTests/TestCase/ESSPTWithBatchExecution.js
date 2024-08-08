import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { scenario } from 'k6/execution';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

const loadProfile = JSON.parse(open('../LiveLogs/filtered.json'));
const essAPI = require('../scripts/ReplayRequest.js');
const logParser = require('../helper/LogParser.js');
const testHelper = require('../helper/LoadStages.js');
const reqSent = new Counter('http_reqs');
const config = JSON.parse(open('../config.json'));
let testStages = testHelper.getLoadStages(loadProfile);
let fixedLogIndex = 1;

export const options = {
  batch: 10000,
  batchPerHost: 6000,
  scenarios: {
    replayLiveLogs: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 60,
      maxDuration: '1h',
    },
  },
};

export function setup() {
  const testStartTime = new Date();
  console.warn("Test Start Time:" + testStartTime.toLocaleTimeString('en-US'));
  console.warn("Test Start Time(UTC):" + testStartTime.toUTCString());
}

export function myTest(delayMap, data) {
  let selectLoadProfile = logParser.getRequestDetailsFromLog(loadProfile, scenario.iterationInTest)
  let reqData = logParser.filterRequestType(selectLoadProfile)
  let LogsDelayProfile = delayMap[scenario.iterationInTest].toFixed(2);
  console.log("Iteration:" + scenario.iterationInTest, "Delay:" + LogsDelayProfile)
  sleep(LogsDelayProfile)
  essAPI.ReplayRequest(reqData);
  reqSent.add(1);
}

export function getLogIndex(index) {
  let currentLogIndex = 0;

  if (index == 0 || index >= testStages.length) {
    return testStages[0];
  }
  else {
    for (let x = 0; x < index; x++) {
      currentLogIndex += testStages[x];
    }
    return currentLogIndex;
  }
}

export function fixedReqRateExecutor() {
  fixedLogIndex += 10;
  console.log("Iteration:" + scenario.iterationInTest, "Request Rate:" + testStages[scenario.iterationInTest], "Start Index:" + fixedLogIndex)
  getRequestBatch(scenario.iterationInTest, fixedLogIndex);
}

export default function testExecutor() {
  fixedReqRateExecutor();
}

export function getRequestBatch(requestRate, logIndex) {
  let batchRequests = [];
  for (let i = 0; i < requestRate; i++) {
    let liveLogRequest = logParser.getRequestDetailsFromLog(loadProfile, i + logIndex)
    let liveRequest = logParser.filterRequestType(liveLogRequest)
    batchRequests.push({
      method: liveRequest.requestMethod,
      url: liveRequest.url,
      body: decodeURI(liveRequest.requestBodyText),
      params: {
        headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" }
      }
    })
  }
  const responseArr = http.batch(batchRequests);
  responseArr.forEach(res => {
    check(res, {
      'HTTP Response: OK': (res) => res.status === 200,
    });
  });
  sleep(60); // Request rate per minutes
}

export function teardown() {
  const testEndTime = new Date();
  console.warn("Test End Time:" + testEndTime.toLocaleTimeString('en-US'));
  console.warn("Test End Time(UTC):" + testEndTime.toUTCString());
}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/PT04June_Script-Live-R2" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/PT04June_Script-Live-R2" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}