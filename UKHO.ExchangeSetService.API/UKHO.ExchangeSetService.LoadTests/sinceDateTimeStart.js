import http from "k6/http";
import { Trend } from 'k6/metrics';
import { sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';

const runTestSinceDateTime = require('./LoadTestForSinceDateTime.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./dataHelper.js');
const apiClient = require('./clientHelper.js');

const sinceDateTimeData_Small = dataHelper.GetSinceDateTimeDataForSmallExchangeSet();
const sinceDateTimeData_Medium = dataHelper.GetSinceDateTimeDataForMediumExchangeSet();
const sinceDateTimeData_Large = dataHelper.GetSinceDateTimeDataForLargeExchangeSet();

let SmallExchangeSetCreationTrend = new Trend('SmallEssCreationtime');
let MediumExchangeSetCreationTrend = new Trend('MediumEssCreationtime');
let LargeExchangeSetCreationTrend = new Trend('LargeEssCreationtime');
let clientAuthResp = {};

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet: {
            exec: 'ESSCreationSmallExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 1,
            iterations: 1,
            maxDuration: '60s'
        },
        ESSCreationMediumExchangeSet: {
            exec: 'ESSCreationMediumExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 1,
            maxDuration: '60s'
        },
        ESSCreationLargeExchangeSet: {
            exec: 'ESSCreationLargeExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 1,
            iterations: 1,
            maxDuration: '60s'
        },
    },
};

export function setup() {
    // client credentials authentication flow
    let essAuthResp = authenticateUsingAzure(
        `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
    );
    clientAuthResp["essToken"] = essAuthResp;

    let fssAuthResp = authenticateusingazure(
        `${config.fss_tenant_id}`, `${config.fss_client_id}`, `${config.fss_client_secret}`, `${config.fss_scopes}`, `${config.fss_resource}`
    );
    clientAuthResp["fssToken"] = fssAuthResp;

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('SmallEssCreation', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Small, "Small");
    });
    SmallExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('MediumEssCreation', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Medium, "Medium");
    });
    MediumExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('LargeEssCreation', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Large, "Large");
    });
    LargeExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function handleSummary(data) {
    return {
        "summary/result.html": htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        "summary/summary.json": JSON.stringify(data),
    }
}