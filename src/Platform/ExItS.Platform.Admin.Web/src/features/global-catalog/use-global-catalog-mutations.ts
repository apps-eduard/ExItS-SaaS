import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  archiveGlobalCatalogTemplate,
  assignGlobalCatalogTemplateProduct,
  bulkAssignGlobalCatalogTemplateProducts,
  bulkRemoveGlobalCatalogTemplateProducts,
  confirmGlobalCatalogImport,
  createGlobalBusinessType,
  createGlobalCatalogTemplate,
  createGlobalCategory,
  createGlobalProduct,
  deleteGlobalProductImage,
  publishGlobalCatalogTemplate,
  removeGlobalCatalogTemplateProduct,
  reorderGlobalCatalogTemplateProducts,
  setGlobalBusinessTypeStatus,
  setGlobalCategoryStatus,
  setGlobalProductStatus,
  unpublishGlobalCatalogTemplate,
  updateGlobalBusinessType,
  updateGlobalCatalogTemplate,
  updateGlobalCatalogTemplateProductFlags,
  updateGlobalCategory,
  updateGlobalProduct,
  uploadGlobalCatalogImport,
  uploadGlobalProductImage,
} from "@/api/global-catalog/global-catalog-client";
import type {
  AssignGlobalCatalogTemplateProductInput,
  BulkAssignGlobalCatalogTemplateProductsInput,
  BulkRemoveGlobalCatalogTemplateProductsInput,
  ConfirmGlobalCatalogImportInput,
  CreateGlobalBusinessTypeInput,
  CreateGlobalCatalogTemplateInput,
  CreateGlobalCategoryInput,
  CreateGlobalProductInput,
  GlobalBusinessTypeStatus,
  GlobalCategoryStatus,
  GlobalProductStatus,
  ReorderGlobalCatalogTemplateProductsInput,
  UpdateGlobalBusinessTypeInput,
  UpdateGlobalCatalogTemplateInput,
  UpdateGlobalCatalogTemplateProductFlagsInput,
  UpdateGlobalCategoryInput,
  UpdateGlobalProductInput,
  UploadGlobalCatalogImportInput,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function useGlobalCatalogMutations() {
  const queryClient = useQueryClient();

  async function invalidateBusinessTypes() {
    await queryClient.invalidateQueries({ queryKey: globalCatalogQueryKeys.businessTypes.all });
  }

  async function invalidateCategories() {
    await queryClient.invalidateQueries({ queryKey: globalCatalogQueryKeys.categories.all });
  }

  async function invalidateProducts() {
    await queryClient.invalidateQueries({ queryKey: globalCatalogQueryKeys.products.all });
  }

  async function invalidateImports() {
    await queryClient.invalidateQueries({ queryKey: globalCatalogQueryKeys.imports.all });
  }

  async function invalidateTemplates() {
    await queryClient.invalidateQueries({ queryKey: globalCatalogQueryKeys.templates.all });
  }

  const createBusinessType = useMutation({
    mutationFn: (input: CreateGlobalBusinessTypeInput) =>
      createGlobalBusinessType(env.platformApiBaseUrl, input),
    onSuccess: invalidateBusinessTypes,
  });

  const updateBusinessType = useMutation({
    mutationFn: ({
      businessTypeId,
      input,
    }: {
      businessTypeId: string;
      input: UpdateGlobalBusinessTypeInput;
    }) => updateGlobalBusinessType(env.platformApiBaseUrl, businessTypeId, input),
    onSuccess: async (_data, variables) => {
      await invalidateBusinessTypes();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.businessTypes.detail(variables.businessTypeId),
      });
    },
  });

  const changeBusinessTypeStatus = useMutation({
    mutationFn: ({
      businessTypeId,
      status,
      expectedUpdatedAtUtc,
    }: {
      businessTypeId: string;
      status: GlobalBusinessTypeStatus;
      expectedUpdatedAtUtc: string;
    }) =>
      setGlobalBusinessTypeStatus(
        env.platformApiBaseUrl,
        businessTypeId,
        status,
        expectedUpdatedAtUtc,
      ),
    onSuccess: async (_data, variables) => {
      await invalidateBusinessTypes();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.businessTypes.detail(variables.businessTypeId),
      });
    },
  });

  const createCategory = useMutation({
    mutationFn: (input: CreateGlobalCategoryInput) =>
      createGlobalCategory(env.platformApiBaseUrl, input),
    onSuccess: invalidateCategories,
  });

  const updateCategory = useMutation({
    mutationFn: ({
      categoryId,
      input,
    }: {
      categoryId: string;
      input: UpdateGlobalCategoryInput;
    }) => updateGlobalCategory(env.platformApiBaseUrl, categoryId, input),
    onSuccess: async (_data, variables) => {
      await invalidateCategories();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.categories.detail(variables.categoryId),
      });
    },
  });

  const changeCategoryStatus = useMutation({
    mutationFn: ({
      categoryId,
      status,
      expectedUpdatedAtUtc,
    }: {
      categoryId: string;
      status: GlobalCategoryStatus;
      expectedUpdatedAtUtc: string;
    }) =>
      setGlobalCategoryStatus(
        env.platformApiBaseUrl,
        categoryId,
        status,
        expectedUpdatedAtUtc,
      ),
    onSuccess: async (_data, variables) => {
      await invalidateCategories();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.categories.detail(variables.categoryId),
      });
    },
  });

  const createProduct = useMutation({
    mutationFn: (input: CreateGlobalProductInput) =>
      createGlobalProduct(env.platformApiBaseUrl, input),
    onSuccess: invalidateProducts,
  });

  const updateProduct = useMutation({
    mutationFn: ({
      productId,
      input,
    }: {
      productId: string;
      input: UpdateGlobalProductInput;
    }) => updateGlobalProduct(env.platformApiBaseUrl, productId, input),
    onSuccess: async (_data, variables) => {
      await invalidateProducts();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.products.detail(variables.productId),
      });
    },
  });

  const changeProductStatus = useMutation({
    mutationFn: ({
      productId,
      status,
      expectedUpdatedAtUtc,
    }: {
      productId: string;
      status: GlobalProductStatus;
      expectedUpdatedAtUtc: string;
    }) =>
      setGlobalProductStatus(
        env.platformApiBaseUrl,
        productId,
        status,
        expectedUpdatedAtUtc,
      ),
    onSuccess: async (_data, variables) => {
      await invalidateProducts();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.products.detail(variables.productId),
      });
    },
  });

  const uploadProductImage = useMutation({
    mutationFn: ({ productId, file }: { productId: string; file: File }) =>
      uploadGlobalProductImage(env.platformApiBaseUrl, productId, file),
    onSuccess: async (_data, variables) => {
      await invalidateProducts();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.products.detail(variables.productId),
      });
    },
  });

  const removeProductImage = useMutation({
    mutationFn: (productId: string) =>
      deleteGlobalProductImage(env.platformApiBaseUrl, productId),
    onSuccess: async (_data, productId) => {
      await invalidateProducts();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.products.detail(productId),
      });
    },
  });

  const uploadImport = useMutation({
    mutationFn: (input: UploadGlobalCatalogImportInput) =>
      uploadGlobalCatalogImport(env.platformApiBaseUrl, input),
    onSuccess: invalidateImports,
  });

  const confirmImport = useMutation({
    mutationFn: ({
      jobId,
      input = {},
    }: {
      jobId: string;
      input?: ConfirmGlobalCatalogImportInput;
    }) => confirmGlobalCatalogImport(env.platformApiBaseUrl, jobId, input),
    onSuccess: async (_data, variables) => {
      await invalidateImports();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.imports.detail(variables.jobId),
      });
    },
  });

  const createTemplate = useMutation({
    mutationFn: (input: CreateGlobalCatalogTemplateInput) =>
      createGlobalCatalogTemplate(env.platformApiBaseUrl, input),
    onSuccess: invalidateTemplates,
  });

  const updateTemplate = useMutation({
    mutationFn: ({
      templateId,
      input,
    }: {
      templateId: string;
      input: UpdateGlobalCatalogTemplateInput;
    }) => updateGlobalCatalogTemplate(env.platformApiBaseUrl, templateId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const publishTemplate = useMutation({
    mutationFn: ({
      templateId,
      expectedUpdatedAtUtc,
    }: {
      templateId: string;
      expectedUpdatedAtUtc?: string;
    }) => publishGlobalCatalogTemplate(env.platformApiBaseUrl, templateId, expectedUpdatedAtUtc),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const unpublishTemplate = useMutation({
    mutationFn: ({
      templateId,
      expectedUpdatedAtUtc,
    }: {
      templateId: string;
      expectedUpdatedAtUtc?: string;
    }) => unpublishGlobalCatalogTemplate(env.platformApiBaseUrl, templateId, expectedUpdatedAtUtc),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const archiveTemplate = useMutation({
    mutationFn: ({
      templateId,
      expectedUpdatedAtUtc,
    }: {
      templateId: string;
      expectedUpdatedAtUtc?: string;
    }) => archiveGlobalCatalogTemplate(env.platformApiBaseUrl, templateId, expectedUpdatedAtUtc),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const assignTemplateProduct = useMutation({
    mutationFn: ({
      templateId,
      input,
    }: {
      templateId: string;
      input: AssignGlobalCatalogTemplateProductInput;
    }) => assignGlobalCatalogTemplateProduct(env.platformApiBaseUrl, templateId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const removeTemplateProduct = useMutation({
    mutationFn: ({
      templateId,
      productId,
      expectedUpdatedAtUtc,
    }: {
      templateId: string;
      productId: string;
      expectedUpdatedAtUtc?: string;
    }) =>
      removeGlobalCatalogTemplateProduct(
        env.platformApiBaseUrl,
        templateId,
        productId,
        expectedUpdatedAtUtc,
      ),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const updateTemplateProductFlags = useMutation({
    mutationFn: ({
      templateId,
      productId,
      input,
    }: {
      templateId: string;
      productId: string;
      input: UpdateGlobalCatalogTemplateProductFlagsInput;
    }) => updateGlobalCatalogTemplateProductFlags(env.platformApiBaseUrl, templateId, productId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const reorderTemplateProducts = useMutation({
    mutationFn: ({
      templateId,
      input,
    }: {
      templateId: string;
      input: ReorderGlobalCatalogTemplateProductsInput;
    }) => reorderGlobalCatalogTemplateProducts(env.platformApiBaseUrl, templateId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const bulkAssignTemplateProducts = useMutation({
    mutationFn: ({
      templateId,
      input,
    }: {
      templateId: string;
      input: BulkAssignGlobalCatalogTemplateProductsInput;
    }) => bulkAssignGlobalCatalogTemplateProducts(env.platformApiBaseUrl, templateId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  const bulkRemoveTemplateProducts = useMutation({
    mutationFn: ({
      templateId,
      input,
    }: {
      templateId: string;
      input: BulkRemoveGlobalCatalogTemplateProductsInput;
    }) => bulkRemoveGlobalCatalogTemplateProducts(env.platformApiBaseUrl, templateId, input),
    onSuccess: async (_data, variables) => {
      await invalidateTemplates();
      await queryClient.invalidateQueries({
        queryKey: globalCatalogQueryKeys.templates.detail(variables.templateId),
      });
    },
  });

  return {
    createBusinessType,
    updateBusinessType,
    changeBusinessTypeStatus,
    createCategory,
    updateCategory,
    changeCategoryStatus,
    createProduct,
    updateProduct,
    changeProductStatus,
    uploadProductImage,
    removeProductImage,
    uploadImport,
    confirmImport,
    createTemplate,
    updateTemplate,
    publishTemplate,
    unpublishTemplate,
    archiveTemplate,
    assignTemplateProduct,
    removeTemplateProduct,
    updateTemplateProductFlags,
    reorderTemplateProducts,
    bulkAssignTemplateProducts,
    bulkRemoveTemplateProducts,
  };
}
