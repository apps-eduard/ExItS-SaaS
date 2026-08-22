import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createGlobalBusinessType,
  createGlobalCategory,
  createGlobalProduct,
  deleteGlobalProductImage,
  setGlobalBusinessTypeStatus,
  setGlobalCategoryStatus,
  setGlobalProductStatus,
  updateGlobalBusinessType,
  updateGlobalCategory,
  updateGlobalProduct,
  uploadGlobalProductImage,
} from "@/api/global-catalog/global-catalog-client";
import type {
  CreateGlobalBusinessTypeInput,
  CreateGlobalCategoryInput,
  CreateGlobalProductInput,
  GlobalBusinessTypeStatus,
  GlobalCategoryStatus,
  GlobalProductStatus,
  UpdateGlobalBusinessTypeInput,
  UpdateGlobalCategoryInput,
  UpdateGlobalProductInput,
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
  };
}
