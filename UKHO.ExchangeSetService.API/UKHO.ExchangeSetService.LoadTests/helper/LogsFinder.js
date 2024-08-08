const Logfile08 = JSON.parse(open('../LiveLogs/ESSPublishedLogs.json'));
const logParser = require('./LogParser.js');

let CellName = new Set();

export default function main() {
  findNextRecord(Logfile08)
}

export function findNextRecord(fileName) {
  let matches = 0
  for (let index = 0; index < fileName.length; index++) {
    let requestObj = logParser.getRequestDetailsFromLog(fileName, index)
    let filterData = logParser.filterByURLContent(requestObj, "webhook/newfilespublished")
    if (filterData != null) {
      let reqBody = JSON.parse(filterData.requestBodyText)
      let reqBodyText = reqBody.data
      let attributes = reqBody.data.attributes
      if (CellName.has(attributes[1].value) == false) {
        CellName.add(attributes[1].value)
        console.log(reqBodyText)
        matches++
      }
      else {
        console.warn(attributes)
      }

    }
  }
  console.log("Total matching record/s :" + matches)
}