using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXON.Penny_JaroWinklerDistance.workers
{
    public class ComparerWorker : BackgroundWorker
    {
        private Boolean flag = false;

        public class Arguments
        {
            public enums.CompareItem cItem;
            public ManualResetEvent resetEvent;
        }


        public class Results
        {
            public string ID { get; set; }
            public string ZdrojObrazu { get; set; }
            public string scannedName { get; set; }
            public string newName { get; set; }
            public double percentage { get; set; }
            public string indexScanned { get; set; }
            public string index1 { get; set; }
            public string streetScanned { get; set; }
            public string street { get; set; }

            public Boolean flag { get; set; }
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {


            Arguments args = (Arguments)e.Argument;


            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {

                int totalCount = 0;
                if (MainForm.phase == false)
                {
                    using (AdresyCZEntities2 aDB = new AdresyCZEntities2())         //PROCESSING CITIES
                    {
                        var cities = aDB.obces.AsEnumerable().Select(r => new { r.Název_obce, r.Kód_obce });
                        var postals = aDB.pscs.AsEnumerable().Select(r => new { r.PSČ, r.Název_obce });
                        var cityParts = aDB.castObces.AsEnumerable().Select(r => new { r.PSČ, r.Název_části_obce });
                        var unrecognizedCities = fcDB.Penny_A5_bw.Where(r => r.MestoNalezeno != true && r.Město != "" && r.Processed != true && r.Ulice != null).Select(r => new { r.Město, r.Zdroj_obrazu, r.ID, r.PSČ, r.Ulice }).Distinct();
                     

                        totalCount = unrecognizedCities.Count();
                        int currentPos = 0;
                        foreach (var unrecognizedCity in unrecognizedCities)
                        {
                            currentPos += 1;
                            args.resetEvent.WaitOne();

                            if (String.IsNullOrWhiteSpace(unrecognizedCity.Město))
                                continue;

                            double PROXIMITY = 0;
                            
                            string UNRECONIZED_CITY = null;
                            string CITY = null;
                            string STREET = null;
                            string ID = null;
                            string unrecognizedStreet = null;
                            int indexFcDb = 0;
                            int num1;
                            string index1 = null;

                            ArrayList arr = new ArrayList();
                            var proxCityName = 0.0;

                            if (int.TryParse(unrecognizedCity.PSČ, out num1))
                            {
                                indexFcDb = Int32.Parse(unrecognizedCity.PSČ);
                            }
                            foreach (var city in cities)
                            {

                                proxCityName = JaroWinklerDistance.proximity(unrecognizedCity.Město.ToUpper().Trim() + "1", city.Název_obce.ToUpper().Trim());

                                if (proxCityName == 1)
                                {
                                    break;
                                }


                                if (proxCityName >= PROXIMITY)
                                {
                                    PROXIMITY = proxCityName;
                                    CITY = city.Název_obce;
                                    ID = unrecognizedCity.ID;

                                    UNRECONIZED_CITY = unrecognizedCity.Město;
                                    unrecognizedStreet = unrecognizedCity.Ulice;

                                }
                                if (PROXIMITY >= 0.96)
                                {
                                    PROXIMITY = 1;
                                    break;
                                }
                                PROXIMITY = Math.Round(PROXIMITY, 2);
                            }
                            if (PROXIMITY >= 0.90 && PROXIMITY < 1)
                            {
                                var allStreets = aDB.allrecs.Where(r => r.PSČ == indexFcDb).Select(r => new { r.PSČ, r.Název_ulice, r.Název_obce });
                                foreach (var str in allStreets)     //check for streets
                                {
                                    double ulicePr = JaroWinklerDistance.proximity(unrecognizedCity.Ulice.ToUpper().Trim() + "  1", str.Název_ulice.ToUpper().Trim());
                                    if (ulicePr >= 0.8)
                                    {
                                        CITY = str.Název_obce;
                                        STREET = str.Název_ulice.ToString();
                                        index1 = str.PSČ.ToString();
                                        PROXIMITY = 1;
                                        break;
                                    }

                                }
                            }

                            if (PROXIMITY < 0.90)
                            {
                                flag = false;

                                var allIndexes = aDB.allrecs.Where(r => r.PSČ == indexFcDb).Select(r => new { r.PSČ, r.Název_ulice, r.Název_obce, r.Název_části_obce });
                                foreach (var str in allIndexes)     //check for city parts within PSC
                                {
                                    double cityPartPr = JaroWinklerDistance.proximity(unrecognizedCity.Město.ToUpper().Trim(), str.Název_části_obce.ToUpper().Trim());

                                    if (cityPartPr >= 0.9)
                                    {
                                        CITY = str.Název_části_obce;
                                        index1 = str.PSČ.ToString();
                                        break;
                                    }
                                    else
                                    {


                                        if (indexFcDb == 0 || indexFcDb == null)    //if index is null
                                        {
                                            var allStreets = aDB.allrecs.Where(r => r.Název_ulice == unrecognizedStreet).Select(r => new { r.PSČ, r.Název_ulice, r.Název_obce, r.Název_části_obce });
                                            foreach (var s in allStreets)     //check for city parts within PSC
                                            {
                                                double streetPart = JaroWinklerDistance.proximity(unrecognizedStreet.ToUpper().Trim(), s.Název_ulice.ToUpper().Trim());
                                                if (streetPart >= 0.9)
                                                {
                                                    CITY = s.Název_části_obce;
                                                    index1 = s.PSČ.ToString();
                                                    STREET = s.Název_ulice;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            double ulicePr = JaroWinklerDistance.proximity(unrecognizedCity.Ulice.ToUpper().Trim() + "  1", str.Název_ulice.ToUpper().Trim());
                                            if (ulicePr > 0.9)
                                            {
                                                CITY = str.Název_obce;
                                                STREET = str.Název_ulice;
                                                index1 = str.PSČ.ToString();
                                                PROXIMITY = 1;
                                                break;
                                            }
                                        }




                                    }

                                }


                            }





                            if (PROXIMITY >= 0.96)
                            {
                                PROXIMITY = 1;
                            }

                            // radio buttons action
                            if (PROXIMITY >= 0.90)
                            {
                                flag = true;
                            }

                            switch (args.cItem)
                            {


                                case enums.CompareItem.Range1:
                                    {
                                        if (0.9 <= PROXIMITY && PROXIMITY < 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range2:
                                    {
                                        if (0.8 <= PROXIMITY && PROXIMITY < 0.9)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range3:
                                    {
                                        if (0.7 <= PROXIMITY && PROXIMITY < 0.8)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range4:
                                    {
                                        if (0.6 <= PROXIMITY && PROXIMITY < 0.7)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range5:
                                    {
                                        if (0.5 <= PROXIMITY && PROXIMITY < 0.6)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range6:
                                    {
                                        if (0.4 <= PROXIMITY && PROXIMITY < 0.5)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range0:
                                    {
                                        if (PROXIMITY <= 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range7:
                                    {
                                        if (PROXIMITY == 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = unrecognizedCity.ID,
                                                    ZdrojObrazu = unrecognizedCity.Zdroj_obrazu,
                                                    scannedName = unrecognizedCity.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = unrecognizedCity.PSČ,
                                                    index1 = index1,
                                                    streetScanned = unrecognizedStreet,
                                                    street = STREET,
                                                    flag = flag

                                                }
                                                );
                                        }
                                        break;
                                    }

                            }



                        }
                    }
                }
                else                                                //PROCESSING STREETS
                {
                    using (AdresyCZEntities2 aDB = new AdresyCZEntities2())
                    {

                        var Cities = fcDB.Penny_A5_bw.Where(r => r.MestoNalezeno == true && r.Město != "" && r.Ulice != "" && r.Processed != true && r.Ulice != null && r.UliceNalezeno != true).Select(r => new { r.Město, r.Zdroj_obrazu, r.ID, r.PSČ, r.Ulice }).Distinct();
                        if (Cities.Count() == 0)
                        { MessageBox.Show(new Form() { TopMost = true }, "There is no checked cities"); }
                        totalCount = Cities.Count();
                        int currentPos = 0;
                        var allStreets = aDB.allrecs.Select(r => new { r.Název_ulice, r.Název_obce, r.Název_části_obce, r.PSČ }).Distinct().ToList();
                        foreach (var City in Cities)
                        {
                            currentPos += 1;
                            args.resetEvent.WaitOne();
                            string CITY = null;
                            string STREET = "";
                            string ID = null;
                            string index1 = "";
                            bool f = false;
                            int num1;
                            double PROXIMITY = 0;
                            int indexFcDb = 0;
                            if (int.TryParse(City.PSČ, out num1))
                            {
                                indexFcDb = Int32.Parse(City.PSČ);
                            }
                            if (String.IsNullOrWhiteSpace(City.Město))
                                continue;

                            foreach (var str in allStreets)     
                            {
                               
                                    double citytPr = JaroWinklerDistance.proximity(City.Město.ToUpper().Trim()+ " 1", str.Název_obce.ToUpper().Trim());
                                    double streetPr = JaroWinklerDistance.proximity(City.Ulice.ToUpper().Trim().Replace(" ", "") + " 1", str.Název_ulice.ToUpper().Trim().Replace(" ", ""));
                                    double cityPartPr = JaroWinklerDistance.proximity(City.Ulice.ToUpper().Trim() + " 1", str.Název_části_obce.ToUpper().Trim());

                                    if (citytPr >=0.9)
                                    {
                                        CITY = str.Název_obce;
                                       
                                         if (str.Název_ulice != "" && streetPr >= 0.9)
                                        {
                                            STREET = str.Název_ulice.ToString();
                                            index1 = str.PSČ.ToString();
                                            PROXIMITY = streetPr;
                                            
                                        }
                                         if (str.Název_ulice != "" && streetPr >= 0.7 && str.PSČ.ToString() == City.PSČ)
                                         {
                                             STREET = str.Název_ulice.ToString();
                                             index1 = str.PSČ.ToString();
                                             PROXIMITY = streetPr;

                                         }
                                         if (str.Název_ulice == "" && cityPartPr >= 0.9)
                                         {
                                             STREET = str.Název_části_obce.ToString();
                                             index1 = str.PSČ.ToString();
                                             PROXIMITY = cityPartPr;
                                            
                                         }
                                         //else 
                                         //{
                                         //    CITY = str.Název_obce.ToString();
                                         //    STREET = str.Název_ulice.ToString();
                                         //    index1 = str.PSČ.ToString();
                                         //    PROXIMITY = streetPr;
                                         //}

                                    }
                                    
                                    
                            }
                            if (PROXIMITY >= 0.95)
                            {
                                PROXIMITY = 1;
                            }
                            PROXIMITY = Math.Round(PROXIMITY, 2);
                            if (PROXIMITY > 0.92)
                            { f = true; }

                            switch (args.cItem)
                            {


                                case enums.CompareItem.Range1:
                                    {
                                        if (0.9 <= PROXIMITY && PROXIMITY < 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range2:
                                    {
                                        if (0.8 <= PROXIMITY && PROXIMITY < 0.9)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range3:
                                    {
                                        if (0.7 <= PROXIMITY && PROXIMITY < 0.8)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range4:
                                    {
                                        if (0.6 <= PROXIMITY && PROXIMITY < 0.7)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range5:
                                    {
                                        if (0.5 <= PROXIMITY && PROXIMITY < 0.6)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range6:
                                    {
                                        if (0.4 <= PROXIMITY && PROXIMITY < 0.5)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range0:
                                    {
                                        if (PROXIMITY <= 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    indexScanned = indexFcDb.ToString(),
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f
                                                }
                                                );
                                        }
                                        break;
                                    }
                                case enums.CompareItem.Range7:
                                    {
                                        if (PROXIMITY <= 1)
                                        {
                                            int percentage = (int)((double)currentPos / (double)totalCount * 100);
                                            this.ReportProgress(percentage, /*string.Format("{0} -> {1} with proximity {2}", UNRECONIZED_CITY, CITY, PROXIMITY)*/
                                                new Results()
                                                {
                                                    ID = City.ID,
                                                    ZdrojObrazu = City.Zdroj_obrazu,
                                                    scannedName = City.Město,
                                                    newName = CITY,
                                                    percentage = PROXIMITY,
                                                    indexScanned = City.PSČ,
                                                    index1 = index1,
                                                    streetScanned = City.Ulice,
                                                    street = STREET,
                                                    flag = f

                                                }
                                                );
                                        }
                                        break;
                                    }
                            }

                        }
                    }

                }
            }
            
        }


        public void save100flag(Dictionary<string, string> ZB_City, Dictionary<string, string> ZB_Prox, bool f)
        {
            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {
                //cmd.CommandTimeout = 60;

                foreach (var entry in ZB_City)
                {
    

                    var unrecognizedCity = fcDB.Penny_A5_bw.Where(r => ZB_City.Keys.Contains(r.Zdroj_obrazu));
                    
                    foreach(var uC in unrecognizedCity)
                    {
                        string PROX = ZB_Prox[uC.Zdroj_obrazu];
                        if (f == false)
                        {
                            uC.MestoNalezeno = true;
                            uC.Processed = true;
                        }
                        else
                        {
                            if (PROX.Trim() == "1")
                            {
                                uC.UliceNalezeno = true;
                                uC.Processed = true;
                            }
                            else 
                            {
                                uC.Processed = true;
                            }
                        }
                    }
                }
                fcDB.SaveChanges();
                //MessageBox.Show("Selected records saved");
                
            }
        }

        public void saveChanges(Dictionary<string, string> ZB_City, Dictionary<string, string> ZB_Prox, bool f)
        {
            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {
                //cmd.CommandTimeout = 60;
                var unrecognizedCities = fcDB.Penny_A5_bw.Where(r => ZB_City.Keys.Contains(r.Zdroj_obrazu));
                foreach (var uCity in unrecognizedCities)
                {
                    string scannedCity = uCity.Město;
                    string city = ZB_City.First(r => r.Key == uCity.Zdroj_obrazu).Value;
                    uCity.MestoNalezeno = true;
                    uCity.Processed = true;
                    string proximity = ZB_Prox.First(r => r.Key == uCity.Zdroj_obrazu).Value;
                    if (f == false)
                    {
                        uCity.Město = city.ToUpper();
                        //uCity.Processed = true;
                        StringBuilder log = new StringBuilder();
                        log.Append(DateTime.Now + "; ");
                        log.Append("Bar code " + uCity.ID.ToString() + "; ");
                        log.Append("Scanned city " + scannedCity + "; ");
                        log.Append("Replaced city " + city.ToUpper() + "; ");
                        log.Append("Proximity " + proximity + "; " + Environment.NewLine);
                        string filePath = "C:\\Logs\\";
                        string fileName = string.Format("log-Cities-{0:yyyy-MM-dd}.txt", DateTime.Now);
                        File.AppendAllText(filePath + fileName, log.ToString());
                        log.Clear();
                    }
                }
                fcDB.SaveChanges();
                MessageBox.Show("Selected records saved");
            }
        }


        public void saveChangesStreets(Dictionary<string, string> ZB_Street, Dictionary<string, string> ZB_Prox, bool f)
        {
            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {
                var unrecognizedStreets = fcDB.Penny_A5_bw.Where(r => ZB_Street.Keys.Contains(r.Zdroj_obrazu));
                foreach (var uStreet in unrecognizedStreets)
                {
                    string scannedStreet = uStreet.Ulice;
                    string street = ZB_Street.First(r => r.Key == uStreet.Zdroj_obrazu).Value;
                    uStreet.UliceNalezeno = true;
                    uStreet.Processed = true;
                    
                    string proximity = ZB_Prox.First(r => r.Key == uStreet.Zdroj_obrazu).Value;
                    if (f == false)
                    {
                        uStreet.Ulice = street.ToUpper();
                        //uStreet.Processed = true;
                        StringBuilder log = new StringBuilder();
                        log.Append(DateTime.Now + "; ");
                        log.Append("Bar code " + uStreet.ID.ToString() + "; ");
                        log.Append("Scanned street " + scannedStreet + "; ");
                        log.Append("Replaced street " + street.ToUpper() + "; ");
                        log.Append("Proximity " + proximity + "; " + Environment.NewLine);
                        string filePath = "C:\\Logs\\";
                        string fileName = string.Format("log-Streets-{0:yyyy-MM-dd}.txt", DateTime.Now);
                        File.AppendAllText(filePath + fileName, log.ToString());
                        log.Clear();
                    }
                }
                fcDB.SaveChanges();
                MessageBox.Show("Selected records saved");
            }
        }
        public void handleUnprocessed(List<string> l, bool f)
        {
            if (f == (false)){
            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {
                var unrecognized = fcDB.Penny_A5_bw.Where(r => l.Contains(r.Zdroj_obrazu));
                foreach (var uCity in unrecognized)
                {
                    uCity.Processed = true;
                }
                fcDB.SaveChanges();
            }
            }
        }
        public void resetProcessed()                        // resets Process flag
        {
            using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
            {
               
                //var unrecognized = fcDB.Penny_A5_bw.Select(r => r);
                var unrCount = fcDB.Penny_A5_bw.Where(r => r.Processed == true).Select(r => new { r.Zdroj_obrazu }).Distinct().Count();
                if (unrCount == 0)
                {
                    MessageBox.Show("All flags for cities are cleared");
                }
                for (int i = 1; i <= unrCount; i++)
                {
                    var unrecognized = fcDB.Penny_A5_bw.Where(r => r.Processed.Value.ToString() == "true").Select(r => new { r.Zdroj_obrazu }).Take(1000);
                    List<string> Proc = new List<string>();
                    foreach (var u in unrecognized)
                    {
                        
                        Proc.Add(u.Zdroj_obrazu);
                       
                    }
                    var unr = fcDB.Penny_A5_bw.Where(r => Proc.Contains(r.Zdroj_obrazu));
                    foreach (var u in unr)
                    {
                        u.Processed = false;
                        i++;
                    }
                    Proc.Clear();       //
                    fcDB.SaveChanges();
                    Thread.Sleep(10);
                    //MessageBox.Show(new Form() { TopMost = true }, "Flag for " + i + " cities is cleared.");
                }
               // fcDB.SaveChanges();
                MessageBox.Show("Processed records reseted");
            }
        }
        public void resetFlag(string s)
        {
            if (s == "Cities")
            {
                using (FlexiCaptureEntities fcDB = new FlexiCaptureEntities())
                
                {
                    var unrCount= fcDB.Penny_A5_bw.Where(r => r.MestoNalezeno.Value.ToString() == "true").Select(r => new { r.Zdroj_obrazu }).Count();

                    if (unrCount == 0)
                    {
                        MessageBox.Show("All flags for cities are cleared");
                    }
                    for(int i = 1; i <= unrCount; i++)
                    {
                    var unrecognized = fcDB.Penny_A5_bw.Where(r => r.MestoNalezeno.Value.ToString() == "true").Select(r => new { r.Zdroj_obrazu }).Take(10000);
                    List<string> Proc = new List<string>();
                    foreach (var u in unrecognized)
                    {
                        Proc.Add(u.Zdroj_obrazu);
                        //if (Proc.Count() == 100)
                        //{
                        //    break;
                        //}
                    }
                    var unr = fcDB.Penny_A5_bw.Where(r => Proc.Contains(r.Zdroj_obrazu));
                    foreach (var u in unr)
                    {
                        u.MestoNalezeno = false;
                        i++;
                    }
                    fcDB.SaveChanges();
                    MessageBox.Show(new Form() { TopMost = true }, "Flag for "+i+" cities is cleared.");
                    }
                }
            }
            if (s == "Streets")
            {
                using (FlexiCaptureEntities fcDBs = new FlexiCaptureEntities())
                {
                    var unrCount = fcDBs.Penny_A5_bw.Where(r => r.UliceNalezeno.Value.ToString() == "true").Select(r => new { r.Zdroj_obrazu }).Count();
                    if (unrCount == 0)
                    {
                        MessageBox.Show("All flags for streets are cleared");
                    }
                    for (int i = 1; i <= unrCount; i++)
                    {
                    var unrecognized = fcDBs.Penny_A5_bw.Where(r => r.UliceNalezeno.Value.ToString() == "true").Select(r => new { r.Zdroj_obrazu }).Take(10000);

                        List<string> Proc = new List<string>();
                        foreach (var u in unrecognized)
                        {
                            Proc.Add(u.Zdroj_obrazu);
                        }
                      
                        var unr = fcDBs.Penny_A5_bw.Where(r => Proc.Contains(r.Zdroj_obrazu));
                        foreach (var u in unr)
                        {
                            u.UliceNalezeno = false;
                            i++;
                            
                        }
                        fcDBs.SaveChanges();
                        
                        MessageBox.Show(new Form() { TopMost = true }, "Flag for " + i+ " streets is cleared.");
                    }
                }
            }
        }
    }
}
