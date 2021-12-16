import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep } from 'k6';
import { Trend } from 'k6/metrics';

const runTestProductIdentifier = require('./scripts/LoadTestForProductIdentifier.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./helper/dataHelper.js');
const apiClient = require('./helper/clientHelper.js');

const apiDownloadClient = require('./scripts/DownloadFileScript.js');

const productIdentifierData_Small_25MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_25MB();
const productIdentifierData_Small_50MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_50MB();
const productIdentifierData_Medium_150MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_150MB();
const productIdentifierData_Medium_300MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_300MB();
const productIdentifierData_Large = dataHelper.GetProductIdentifierDataforLargeExchangeSet();

let SmallExchangeSetCreationTrendfor25MB = new Trend('SmallEssCreationtimefor25MB');
let SmallExchangeSetCreationTrendfor50MB = new Trend('SmallEssCreationtimefor50MB');
let MediumExchangeSetCreationTrendfor150MB = new Trend('MediumEssCreationtimefor150MB');
let MediumExchangeSetCreationTrendfor300MB = new Trend('MediumEssCreationtimefor300MB');
let LargeExchangeSetCreationTrend = new Trend('LargeEssCreationtime');

let SmallExchangeSetDownloadTrendfor25MB = new Trend('SmallEssDownloadtimefor25MB');
let SmallExchangeSetDownloadTrendfor50MB = new Trend('SmallEssDownloadtimefor50MB');
let MediumExchangeSetDownloadTrendfor150MB = new Trend('MediumEssDownloadtimefor150MB');
let MediumExchangeSetDownloadTrendfor300MB = new Trend('MediumEssDownloadtimefor300MB');
let LargeExchangeSetDownloadTrend = new Trend('LargeEssDownloadtime');

let SmallExchangeSetEndtoEndTrendfor25MB = new Trend('SmallEssEndtoEndtimefor25MB');
let SmallExchangeSetEndtoEndTrendfor50MB = new Trend('SmallEssEndtoEndtimefor50MB');
let MediumExchangeSetEndtoEndTrendfor150MB = new Trend('MediumEssEndtoEndtimefor150MB');
let MediumExchangeSetEndtoEndTrendfor300MB = new Trend('MediumEssEndtoEndtimefor300MB');
let LargeExchangeSetEndtoEndTrend = new Trend('LargeEssEndtoEndtime');

let clientAuthResp = {};
let fssDetailsResponse, fssFileName, fileName ="V01X01.zip";
let downloadFlag = config.DownloadFile.DownloadFlag;

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet_25MB: {
            exec: 'ESSCreationSmallExchangeSet_25MB',
            executor: 'per-vu-iterations',
            startTime: '5s',
            gracefulStop: '5s',
            vus: 3,
            iterations: 25,
            maxDuration: '1h'
        },

        ESSCreationSmallExchangeSet_50MB: {
            exec: 'ESSCreationSmallExchangeSet_50MB',
            executor: 'per-vu-iterations',
            startTime: '15s',
            gracefulStop: '5s',
            vus: 2,
            iterations: 25,
            maxDuration: '1h'
        },

        ESSCreationMediumExchangeSet_150MB: {
            exec: 'ESSCreationMediumExchangeSet_150MB',
            executor: 'per-vu-iterations',
            startTime: '30s',
            gracefulStop: '5s',
            vus: 2,
            iterations: 10,
            maxDuration: '1h'
        },

        ESSCreationMediumExchangeSet_300MB: {
            exec: 'ESSCreationMediumExchangeSet_300MB',
            executor: 'per-vu-iterations',
            startTime: '1m',
            gracefulStop: '5s',
            vus: 1,
            iterations: 5,
            maxDuration: '1h'
        },

        ESSCreationLargeExchangeSet: {
            exec: 'ESSCreationLargeExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '4m',
            gracefulStop: '5s',
            vus: 2,
            iterations: 3,
            maxDuration: '1h'
        },
    },
};

export function setup() {
    // client credentials authentication flow
    let essAuthResp = authenticateUsingAzure(
        `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
    );
    clientAuthResp["essToken"] = essAuthResp.access_token;

    let fssAuthResp = authenticateUsingAzure(
        `${config.FSS_TENANT_ID}`, `${config.FSS_CLIENT_ID}`, `${config.FSS_CLIENT_SECRET}`, `${config.FSS_SCOPES}`, `${config.FSS_RESOURCE}`
    );
    clientAuthResp["fssToken"] = fssAuthResp.access_token;

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet_25MB(clientAuthResp) {
  var group_duration = apiClient.GetGroupDuration('Small_25MB_EssCreation', () => {
    fssDetailsResponse = runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_25MB, "Small_25MB");
  });
  SmallExchangeSetCreationTrendfor25MB.add(group_duration);
  
  fssFileName = fssDetailsResponse['files'][0]['filename'];

  if (downloadFlag && fileName == fssFileName)
  {
    var group_durationFileDownload = apiClient.GetGroupDuration('Small_25MB_EssCreation', () => {    
      apiDownloadClient.fileDownload(`${clientAuthResp.fssToken}`, fssDetailsResponse);
    });  
    SmallExchangeSetDownloadTrendfor25MB.add(group_durationFileDownload);
    
    var group_durationTotal = group_duration + group_durationFileDownload
    SmallExchangeSetEndtoEndTrendfor25MB.add(group_durationTotal);
  }
  sleep(1);
}

export function ESSCreationSmallExchangeSet_50MB(clientAuthResp) {
  
  var group_duration = apiClient.GetGroupDuration('Small_50MB_EssCreation', () => {
    fssDetailsResponse = runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_50MB, "Small_50MB");
  });
  SmallExchangeSetCreationTrendfor50MB.add(group_duration);

  fssFileName = fssDetailsResponse['files'][0]['filename'];

  if(downloadFlag && fileName == fssFileName)
  {
    var group_durationFileDownload = apiClient.GetGroupDuration('Small_50MB_EssCreation', () => {    
      apiDownloadClient.fileDownload(`${clientAuthResp.fssToken}`, fssDetailsResponse);
    });  
    SmallExchangeSetDownloadTrendfor50MB.add(group_durationFileDownload);
  
    var group_durationTotal = group_duration + group_durationFileDownload
    SmallExchangeSetEndtoEndTrendfor50MB.add(group_durationTotal);
  }
  sleep(1);
}

export function ESSCreationMediumExchangeSet_150MB(clientAuthResp) {
  var group_duration = apiClient.GetGroupDuration('Medium_150MB_EssCreation', () => {
    fssDetailsResponse = runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_150MB, "Medium_150MB");
  });
  MediumExchangeSetCreationTrendfor150MB.add(group_duration);

  fssFileName = fssDetailsResponse['files'][0]['filename'];

  if(downloadFlag && fileName == fssFileName)
  {
    var group_durationFileDownload = apiClient.GetGroupDuration('Medium_150MB_EssCreation', () => {    
      apiDownloadClient.fileDownload(`${clientAuthResp.fssToken}`, fssDetailsResponse);
    });  
    MediumExchangeSetDownloadTrendfor150MB.add(group_durationFileDownload);
  
    var group_durationTotal = group_duration + group_durationFileDownload
    MediumExchangeSetEndtoEndTrendfor150MB.add(group_durationTotal);
  }
  sleep(1);
}

export function ESSCreationMediumExchangeSet_300MB(clientAuthResp) {
  var group_duration = apiClient.GetGroupDuration('Medium_300MB_EssCreation', () => {
    fssDetailsResponse = runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_300MB, "Medium_300MB");
  });
  MediumExchangeSetCreationTrendfor300MB.add(group_duration);

  fssFileName = fssDetailsResponse['files'][0]['filename'];

  if(downloadFlag && fileName == fssFileName)
  {
    var group_durationFileDownload = apiClient.GetGroupDuration('Medium_300MB_EssCreation', () => {    
      apiDownloadClient.fileDownload(`${clientAuthResp.fssToken}`, fssDetailsResponse);
    });  
    MediumExchangeSetDownloadTrendfor300MB.add(group_durationFileDownload);
  
    var group_durationTotal = group_duration + group_durationFileDownload
    MediumExchangeSetEndtoEndTrendfor300MB.add(group_durationTotal);
  }
  sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
  var group_duration = apiClient.GetGroupDuration('LargeEssCreation', () => {
    fssDetailsResponse = runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Large, "Large");
  });
  LargeExchangeSetCreationTrend.add(group_duration);

  fssFileName = fssDetailsResponse['files'][0]['filename'];

  if(downloadFlag && fileName == fssFileName)
  {
    var group_durationFileDownload = apiClient.GetGroupDuration('LargeEssCreation', () => {    
      apiDownloadClient.fileDownload(`${clientAuthResp.fssToken}`, fssDetailsResponse);
    });  
    LargeExchangeSetDownloadTrend.add(group_durationFileDownload);
  
    var group_durationTotal = group_duration + group_durationFileDownload
    LargeExchangeSetEndtoEndTrend.add(group_durationTotal);
  }
  sleep(1);
}

export function handleSummary(data) {
  return {
    ["summary/ProductIdentifierResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["summary/ProductIdentifierResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}