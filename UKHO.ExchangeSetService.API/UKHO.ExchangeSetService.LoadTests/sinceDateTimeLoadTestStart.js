import { sleep, group } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';

const runTestSinceDateTime = require('./scripts/LoadTestForSinceDateTime.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./helper/dataHelper.js');

const sinceDateTimeData_Small = dataHelper.GetSinceDateTimeDataForSmallExchangeSet();
const sinceDateTimeData_Medium = dataHelper.GetSinceDateTimeDataForMediumExchangeSet();
const sinceDateTimeData_Large = dataHelper.GetSinceDateTimeDataForLargeExchangeSet();

let clientAuthResp = {};

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet: {
            exec: 'ESSCreationSmallExchangeSet',
            executor: 'ramping-vus',
            stages: [
                { duration: '5m', target: 10 },
                { duration: '5m', target: 10 },
                { duration: '5m', target: 30 },
                { duration: '35m', target: 30 },
                { duration: '5m', target: 20 },
                { duration: '5m', target: 0 }
            ]
        },
        // ESSCreationMediumExchangeSet: {
        //     exec: 'ESSCreationMediumExchangeSet',
        //     executor: 'constant-arrival-rate',
        //     rate: 3,
        //     timeUnit: '2s',
        //     duration: '1h',
        //     preAllocatedVUs: 64,
        //     maxVUs: 70
        // },
        // ESSCreationLargeExchangeSet: {
        //     exec: 'ESSCreationLargeExchangeSet',
        //     executor: 'constant-arrival-rate',
        //     rate: 3,
        //     timeUnit: '2s',
        //     duration: '1h',
        //     preAllocatedVUs: 64,
        //     maxVUs: 70
        // },
    },
};

export function setup() {
    // client credentials authentication flow
     let essAuthResp = authenticateUsingAzure(
         `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
     );
     clientAuthResp["essToken"] = essAuthResp.access_token;
    
    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet(clientAuthResp) {
    group('SmallEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Small, "Small");
    });
    sleep(1);
}

export function ESSCreationMediumExchangeSet(clientAuthResp) {
    group('MediumEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Medium, "Medium");
    });
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    group('LargeEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Large, "Large");
    });
    sleep(1);
}

export function handleSummary(data) {
    return {
        ["summary/SinceDataTimeResult_"+new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]:htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        ["summary/SinceDataTimeResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]:JSON.stringify(data),
    }
}