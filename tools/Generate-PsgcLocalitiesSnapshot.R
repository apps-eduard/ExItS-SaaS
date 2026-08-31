# Generates ExItS PSGC City/Municipality snapshot from an official-sourced release table.
# Preferred official source: PSA PSGC Publication Datafile (2Q 2026 / as of 30 June 2026).
# This helper currently extracts City+Municipality rows from a local CRAN/GitHub `psgc`
# Windows binary that bundles the PSA Q2_2026 release when psa.gov.ph is unreachable
# (e.g. Cloudflare). Runtime ExItS never calls PSA.
#
# Usage (from repo root, after placing binary package under .tmp-psgc-win/psgc):
#   Rscript tools/Generate-PsgcLocalitiesSnapshot.R
#   python tools/Convert-PsgcLocalitiesCsvToJson.py

options(warn = 1)
root <- if (nzchar(Sys.getenv("EXITS_ROOT"))) Sys.getenv("EXITS_ROOT") else getwd()
setwd(root)

lib <- file.path(tempdir(), "psgc-lib")
dir.create(lib, showWarnings = FALSE, recursive = TRUE)
bin <- ".tmp-psgc-win/psgc"
if (!dir.exists(file.path(lib, "psgc"))) {
  if (!dir.exists(bin)) {
    stop("Missing .tmp-psgc-win/psgc — download/install the psgc Windows binary first.")
  }
  stopifnot(file.copy(bin, lib, recursive = TRUE))
}
.libPaths(c(lib, .libPaths()))
library(psgc)

release <- "Q2_2026"
all_df <- get_psgc(release = release)
reg_map <- setNames(all_df$area_name[all_df$geographic_level == "Reg"], all_df$psgc_code[all_df$geographic_level == "Reg"])
prov_map <- setNames(all_df$area_name[all_df$geographic_level == "Prov"], all_df$psgc_code[all_df$geographic_level == "Prov"])
locs <- all_df[all_df$geographic_level %in% c("City", "Mun"), ]

region_code <- paste0(substr(locs$psgc_code, 1, 2), "00000000")
is_province_shaped <- substr(locs$psgc_code, 6, 10) == "00000"
candidate_province <- ifelse(is_province_shaped, NA_character_, paste0(substr(locs$psgc_code, 1, 5), "00000"))
province_name <- ifelse(is.na(candidate_province), NA_character_, unname(prov_map[candidate_province]))
province_code <- ifelse(is.na(province_name) | !nzchar(province_name), NA_character_, candidate_province)
region_name <- unname(reg_map[region_code])

out <- data.frame(
  psgcCode = as.character(locs$psgc_code),
  name = as.character(locs$area_name),
  localityType = ifelse(locs$geographic_level == "City", "City", "Municipality"),
  regionCode = as.character(region_code),
  regionName = as.character(region_name),
  provinceCode = province_code,
  provinceName = province_name,
  stringsAsFactors = FALSE
)
out <- out[order(out$psgcCode, out$name), ]
rownames(out) <- NULL
stopifnot(!anyDuplicated(out$psgcCode))

dir.create("tools/.generated", showWarnings = FALSE, recursive = TRUE)
utils::write.csv(out, "tools/.generated/psgc-localities-2026-06-30.csv", row.names = FALSE, na = "")
cat("Wrote tools/.generated/psgc-localities-2026-06-30.csv rows=", nrow(out), "\n")
cat("Next: python tools/Convert-PsgcLocalitiesCsvToJson.py\n")
