﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Lagou.Repository;
using Newtonsoft.Json;
using Lagou.Repository.Entity;
using System.Text.RegularExpressions;

namespace Lagou.Web.Controllers
{

    public class LagouDataController : ApiController
    {
        private JobRepository repository;
        public LagouDataController()
        {
            repository = new JobRepository();
        }

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }


        /// <summary>
        /// 城市职位需求总数与公司总数
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string QueryJCityNeedJobNum()
        {
            var jobCount = repository.QueryJCityNeedJobNum();
            var companyCount = repository.QueryCityCompanyNum();

            jobCount.ForEach(o =>
            {
                var company = companyCount.FirstOrDefault(c => c.City == o.City);
                if (company != null)
                {
                    o.CompanyNum = company.CompanyNum;
                }
            });
            var json = JsonConvert.SerializeObject(jobCount);

            return json;
        }

        /// <summary>
        /// 城市对某一职位的需求数据
        /// </summary>
        /// <param name="positionName"></param>
        /// <returns></returns>
        [HttpGet]
        public string QueryPositionNum(string positionName)
        {
            var jobCount = repository.QueryPositionNum(positionName);

            return JsonConvert.SerializeObject(jobCount);

        }

        /// <summary>
        /// 城市对各个工作年限段的人数需求
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string QueryWorkYearJobNum()
        {
            var results = repository.QueryWorkYearJobNum();
            var workYearList = new List<WorkYearSalaryEntity>();
            List<IGrouping<string, WorkYearSalaryEntity>> group = results.GroupBy(o => o.City).ToList();
            List<string> citys = new List<string>();
            foreach (var item in group)
            {
                string city = item.Key;
                citys.Add(city);
            }

            foreach (var city in citys)
            {
                foreach (var item in results)
                {
                    if (item.City == city)
                    {
                        string workyear = getWorkYear(item.WorkYear);
                        if (workYearList.Any(o => o.WorkYear == workyear && o.City == city))
                        {
                            var obj = workYearList.FirstOrDefault(o => o.WorkYear == workyear);
                            obj.JobNum += item.JobNum;
                        }
                        else
                        {
                            workYearList.Add(new WorkYearSalaryEntity
                            {
                                City = item.City,
                                JobNum = item.JobNum,
                                WorkYear = workyear
                            });
                        }
                    }
                }
            }
            /*
            xdata:['北京','深圳','上海'],
            ydata:[{name:"1-3年",type:'bar',data:['北京1-3数据','深圳1-3年数据',上海1-3年数据']}]
            */
            List<WorkYearJobNumModel> jsonResult = new List<WorkYearJobNumModel>();
            var workyears = new List<string> {
                "1年以下",
                "1-3年",
                "3-5年",
                "5-10年",
                "10年以上",
                "不限"
            };
            workyears.ForEach(o =>
            {
                jsonResult.Add(new WorkYearJobNumModel
                {
                    name = o,
                    type = "bar",
                    data = new List<int>(),
                });
            });


            foreach (var city in citys)
            {

                foreach (var workyear in workyears)
                {
                    var obj = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == workyear);
                    if (obj != null)
                    {
                        var year = jsonResult.FirstOrDefault(o => o.name == workyear);
                        if (year != null)
                        {
                            year.data.Add(obj.JobNum);
                        }
                        else
                        {
                            year.data.Add(0);
                        }
                    }
                }
                //var workyear1 = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "1年以下");
                //var year1 = jsonResult.FirstOrDefault(o => o.name == "1年以下");
                //year1.data.Add(workyear1.JobNum);

                //var workyear3 = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "1-3年");
                //var year3 = jsonResult.FirstOrDefault(o => o.name == "1-3年");
                //year3.data.Add(workyear3.JobNum);

                //var workyear5 = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "3-5年");
                //var year5 = jsonResult.FirstOrDefault(o => o.name == "3-5年");
                //year5.data.Add(workyear5.JobNum);

                //var workyear10 = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "5-10年");
                //var year10 = jsonResult.FirstOrDefault(o => o.name == "5-10年");
                //year10.data.Add(workyear10.JobNum);

                //var workyear10up = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "10年以上");
                //var year10up = jsonResult.FirstOrDefault(o => o.name == "10年以上");
                //year10up.data.Add(workyear10up.JobNum);

                //var workyearno = workYearList.FirstOrDefault(o => o.City == city && o.WorkYear == "不限");
                //var yearno = jsonResult.FirstOrDefault(o => o.name == "不限");
                //yearno.data.Add(workyearno.JobNum);


            }

            var json = new
            {
                xdata = citys,
                ydata = jsonResult
            };


            return JsonConvert.SerializeObject(json);
        }

        /// <summary>
        ///各个城市同一职位的薪水
        /// </summary>
        /// <param name="positionName"></param>
        /// <returns></returns>
        [HttpGet]
        public string QueryPositionNameSalary(string positionName)
        {
            var results = repository.QueryPositionNameSalary(positionName);
            var salaryList = new List<WorkYearSalaryEntity>();
            List<IGrouping<string, WorkYearSalaryEntity>> group = results.GroupBy(o => o.City).ToList();
            List<string> citys = new List<string>();
            foreach (var item in group)
            {
                string city = item.Key;
                citys.Add(city);
            }

            //遍历城市 
            foreach (var city in citys)
            {
                foreach (var item in results)
                {
                    if (item.City.Equals(city))
                    {
                        string salary = getSalaryRange(item.Salary);
                        if (salaryList.Any(o => o.Salary == salary && o.City == item.City))
                        {
                            var salaryObj = salaryList.FirstOrDefault(o => o.Salary == salary);
                            salaryObj.JobNum += item.JobNum;
                        }
                        else
                        {
                            salaryList.Add(new WorkYearSalaryEntity
                            {
                                City = city,
                                Salary = salary,
                                JobNum = item.JobNum
                            });
                        }
                    }
                }
            }

            var jsonResult = new List<WorkYearJobNumModel>();
            var salrayArea = new List<string>()
            {
                "0k-5k",
                "6k-10k",
                "11k-15k",
                "16k-20k",
                "21k-25k",
                "26k-30k",
                "30k以上"
            };
            salrayArea.ForEach(o =>
            {
                jsonResult.Add(new WorkYearJobNumModel
                {
                    name = o,
                    type = "bar",
                    data = new List<int>()
                });
            });

            foreach (var city in citys)
            {
                foreach (var item in salrayArea)
                {
                    var obj = salaryList.FirstOrDefault(o => o.City == city && o.Salary == item);
                    if (obj != null)
                    {
                        var resultObj = jsonResult.FirstOrDefault(o => o.name == item);
                        resultObj.data.Add(obj.JobNum);
                    }
                    else
                    {
                        var resultObj = jsonResult.FirstOrDefault(o => o.name == item);
                        resultObj.data.Add(0);
                    }
                }
            }

            var json = new
            {
                xdata = citys,
                ydata = jsonResult
            };

            return JsonConvert.SerializeObject(json);
        }


        /// <summary>
        /// 行业薪水分布
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string QueryIndustrySalary()
        {
            var industryList = new List<IndustrySalarEntity>();
            var result = repository.QueryIndustrySalary();

            //拆分多行业标签的公司 
            foreach(var o in result)
            {
                if (o.Industry.Contains('·'))
                {
                    var industryArray = o.Industry.Split('·');
                    string industryArray1 = industryArray[0].Trim();
                    string industryArray2 = industryArray[1].Trim();
                    var industryObj = industryList.FirstOrDefault(c => c.Industry == industryArray1);
                    var industryObj2 = industryList.FirstOrDefault(c => c.Industry == industryArray2);
                    if (industryObj != null)
                    {
                        industryObj.Num += o.Num;
                    }
                    else
                    {
                        industryList.Add(new IndustrySalarEntity
                        {
                            Industry = industryArray1,
                            Num = o.Num
                        });
                    }
                    if (industryObj2 != null)
                    {
                        industryObj2.Num += o.Num;
                    }
                    else
                    {
                        industryList.Add(new IndustrySalarEntity
                        {
                            Industry = industryArray2,
                            Num = o.Num
                        });
                    }
                }
                else {
                    //单行业标签
                    var obj = industryList.FirstOrDefault(c => c.Industry == o.Industry);
                    if (obj != null)
                    {
                        obj.Num += o.Num;
                    }
                    else
                    {
                        industryList.Add(new IndustrySalarEntity
                        {
                            Industry = o.Industry,
                            Num = o.Num
                        });
                    }
                }

            };

            /*
            * 取前10的行业然后，查询该行业的薪水分布
            */

            industryList=  industryList.OrderByDescending(o => o.Num).ToList();

            var industryNames = new List<string>();
            var jsonResult = new List<WorkYearSalaryEntity>();
            var industrySalarys = new List<WorkYearJobNumModel>();
            var salrayArea = new List<string>()
            {
                "0k-5k",
                "6k-10k",
                "11k-15k",
                "16k-20k",
                "21k-25k",
                "26k-30k",
                "30k以上"
            };

            salrayArea.ForEach(o => {
                industrySalarys.Add(new WorkYearJobNumModel
                {
                    name = o,
                    type = "bar",
                    data = new List<int>()
                });
            });

            for (int i = 0; i < 10; i++)
            {
                //取前10的行业
                //取每个行业的薪水段
                //ydata:[{name:"1-3年",type:"bar",data:[12,21,2,20]}/*x轴数据*/]
                //industryList[i];
                //行业薪水
                industryNames.Add(industryList[i].Industry);

                var salaryList = repository.QueryIndustrySalary(industryList[i].Industry);
                foreach(var o in salaryList)
                {

                    string salary = getSalaryRange(o.Salary);
                    var obj = jsonResult.FirstOrDefault(c => c.Salary.Equals(salary)&&c.City==industryList[i].Industry);
                    if (obj != null)
                    {
                        obj.JobNum += o.Num;
                    }
                    else
                    {
                        jsonResult.Add(new WorkYearSalaryEntity
                        {
                            City = industryList[i].Industry,
                            Salary = salary,
                            JobNum = o.Num
                        });
                    }
                    //结果
                    /*
                    {city:"移动互联网",Salary:"10-15K" JobNum:20"}
                    {city:"移动互联网",Salary:"15-20K" JobNum:30}
                    */
                };

            }


            foreach(var o in industryNames)
            {
                //遍历某一行业下的薪水段
                foreach(var c in salrayArea)
                {
                    //取出该段的薪水对象，将该段的薪水值添加到 data中 
                    var salaryObj = jsonResult.FirstOrDefault(k => k.City == o && k.Salary == c);
                    if (salaryObj != null)
                    {
                        var jsonObj = industrySalarys.FirstOrDefault(m => m.name == c);
                        jsonObj.data.Add(salaryObj.JobNum);
                    }
                    else
                    {
                        //此处逻辑有问题 只有第一条有记录，它没有
                        var jsonObj = industrySalarys.FirstOrDefault(m => m.name == c);
                        jsonObj.data.Add(0);
                    }
                };

            };

            var json = new
            {
                xdata = industryNames,
                ydata = industrySalarys
            };

            return JsonConvert.SerializeObject(json);

            /*
                最终数据格式
                {name:"移动互联网",type:"bar" data:[10,20,30,30]/移动互联网，电子商务，O2O/}
            */



            //return string.Empty;
        }

        /// <summary>
        /// 划定薪水区间
        /// </summary>
        /// <param name="salary"></param>
        private string getSalaryRange(string salary)
        {
            /*
             *0k-5K
             *6k-10K
             *11k-15K
             *16k-20K
             *21k-25K
             *26k-30K
             *30k以上
             */
            Regex regex;
            if (!string.IsNullOrEmpty(salary))
            {
                regex = new Regex(@"(?<salary>(?<=k-)\d+)");
                string salaryValue = regex.Match(salary).Groups["salary"].Value;
                int value = string.IsNullOrEmpty(salaryValue) ? 0 : Convert.ToInt32(salaryValue);
                return getRange(value);
            }

            if (salary.Contains("以上"))
            {
                regex = new Regex(@"(?<salary>\d+(?=\D))", RegexOptions.Singleline);
                string salaryValue = regex.Match(salary).Groups["salary"].Value;
                int value = string.IsNullOrEmpty(salaryValue) ? 0 : Convert.ToInt32(salaryValue);
                return getRange(value);
            }
            else if (salary.Contains("以下"))
            {
                regex = new Regex(@"(?<salary>\d+(?=\D))", RegexOptions.Singleline);
                string salaryValue = regex.Match(salary).Groups["salary"].Value;
                int value = string.IsNullOrEmpty(salaryValue) ? 0 : Convert.ToInt32(salaryValue);
                return getRange(value);
            }

            return string.Empty;
        }

        private string getRange(int salary)
        {
            /*
            *0k-5K
            *6k-10K
            *11k-15K
            *16k-20K
            *21k-25K
            *26k-30K
            *30k以上
            */
            //未确定范围 **以上  的类型
            if (salary == 0)
            {
                return "0k-5k";
            }
            else if (salary > 0 && salary <= 5)
            {
                return "0k-5k";
            }
            else if (salary > 5 && salary <= 10)
            {
                return "6k-10k";
            }
            else if (salary > 10 && salary <= 15)
            {
                return "11k-15k";
            }
            else if (salary > 15 && salary <= 20)
            {
                return "16k-20k";
            }
            else if (salary > 20 && salary <= 25)
            {
                return "21k-25k";
            }
            else if (salary > 20 && salary <= 25)
            {
                return "26k-30k";
            }
            else
            {
                return "30k以上";
            }
        }

        /// <summary>
        /// 取工作年限  
        /// </summary> 
        /// <param name="year">年限1以下 1-3 3-5 5-10 10以上 不限</param>
        /// <returns></returns>
        private string getWorkYear(string year)
        {
            switch (year)
            {
                case "应届毕业生":
                    return "1年以下";
                case "1年以下":
                    return "1年以下";
                case "无经验年":
                    return "1年以下";
                default:
                    return year;
            }
        }


    }
}